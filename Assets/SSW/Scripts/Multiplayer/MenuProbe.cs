#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SSW
{
    public sealed class MenuProbe : MonoBehaviour
    {
        [Serializable] sealed class Command
        {
            public int seq;
            public string op;
            public string target;
            public string text;
            public int value;
        }

        [Serializable] sealed class Control
        {
            public string name;
            public string text;
            public bool enabled;
        }

        [Serializable] sealed class Panel
        {
            public string name;
            public bool active;
            public float alpha;
            public bool interactive;
        }

        [Serializable] sealed class State
        {
            public int seq;
            public string error;
            public string scene;
            public string status;
            public bool busy;
            public bool matching;
            public bool joined;
            public bool host;
            public bool canStart;
            public string room;
            public string code;
            public int count;
            public string roomId;
            public string profile;
            public bool signedIn;
            public bool listening;
            public bool server;
            public int sessions;
            public int protocol;
            public string config;
            public Room[] rooms;
            public Control[] buttons;
            public Control[] inputs;
            public Panel[] panels;
        }

        [Serializable] sealed class Room
        {
            public string id;
            public string name;
            public int players;
        }

        string _path;
        int _sequence;
        float _next;
        string _error = "";
        bool _drive;
        bool _signingIn;

        public void Init(string path, bool drive = true)
        {
            _drive = drive;
            _path = Path.GetFullPath(path) + ".menu";
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
        }

        void Update()
        {
            if (_path == null || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.1f;
            try
            {
                string input = _path + ".cmd.json";
                if (File.Exists(input))
                {
                    Command command = JsonUtility.FromJson<Command>(File.ReadAllText(input));
                    if (command.seq > _sequence)
                    {
                        _sequence = command.seq;
                        Execute(command);
                    }
                }
                Write();
            }
            catch (IOException) { }
            catch (Exception exception) { _error = exception.Message; Write(); }
#if UNITY_EDITOR
            if (_drive) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        static IEnumerable<T> Components<T>() where T : Component
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        void Execute(Command command)
        {
            switch (command.op)
            {
                case "click":
                    var button = Components<UnityEngine.UI.Button>().Single(item =>
                        item.name == command.target && item.gameObject.activeInHierarchy && item.IsInteractable());
                    Canvas.ForceUpdateCanvases();
                    var rect = (RectTransform)button.transform;
                    var canvas = button.GetComponentInParent<Canvas>();
                    var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    var pointer = new PointerEventData(EventSystem.current)
                    {
                        position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                        button = PointerEventData.InputButton.Left
                    };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                        throw new InvalidOperationException("Button is obscured: " + command.target);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    break;
                case "text":
                    Components<UnityEngine.UI.InputField>().Single(item =>
                        item.name == command.target && item.gameObject.activeInHierarchy).text = command.text;
                    break;
                case "toggle":
                    Components<UnityEngine.UI.Toggle>().Single(item =>
                        item.name == command.target && item.gameObject.activeInHierarchy).isOn = command.value > 0;
                    break;
                case "job": PlayerJobStorage.Save((PlayerJob)command.value); break;
                case "capture": ScreenCapture.CaptureScreenshot(_path + ".png"); break;
                case "clear": _error = ""; break;
                case "profile": _ = SignIn(command.text); break;
                case "delete": _ = DeleteRoom(command.text); break;
            }
        }

        async Task SignIn(string profile)
        {
            var sessions = MultiplayerSessionManager.GetOrCreate();
            if (_signingIn || sessions.IsInSession || sessions.IsBusy)
            {
                _error = "Close the test session before changing profile";
                return;
            }
            _signingIn = true;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(profile));
                var auth = AuthenticationService.Instance;
                if (auth.Profile != profile)
                {
                    auth.SignOut();
                    auth.SwitchProfile(profile);
                }
                if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();
            }
            catch (Exception error) { _error = error.Message; }
            finally { _signingIn = false; }
        }

        async Task DeleteRoom(string id)
        {
            try
            {
                var lobby = await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(id);
                if (lobby.HostId != AuthenticationService.Instance.PlayerId)
                    throw new InvalidOperationException("The test profile does not own this room");
                await Unity.Services.Lobbies.LobbyService.Instance.DeleteLobbyAsync(id);
            }
            catch (Unity.Services.Lobbies.LobbyServiceException error) when
                (error.Reason == Unity.Services.Lobbies.LobbyExceptionReason.LobbyNotFound) { }
            catch (Exception error) { _error = error.Message; }
        }

        void Write()
        {
            var sessions = MultiplayerSessionManager.Current;
            bool initialized = UnityServices.State == ServicesInitializationState.Initialized;
            var network = NetGame.Current.Manager;
            var buttons = Components<UnityEngine.UI.Button>()
                .Where(button => button.gameObject.activeInHierarchy)
                .Select(button => new Control
                {
                    name = button.name,
                    text = button.GetComponentInChildren<UnityEngine.UI.Text>()?.text ?? "",
                    enabled = button.IsInteractable()
                }).ToArray();
            var inputs = Components<UnityEngine.UI.InputField>()
                .Where(input => input.gameObject.activeInHierarchy)
                .Select(input => new Control { name = input.name, text = input.contentType == UnityEngine.UI.InputField.ContentType.Password ? "" : input.text, enabled = input.IsInteractable() }).ToArray();
            var panels = Components<CanvasGroup>().Select(panel => new Panel
            {
                name = panel.name, active = panel.gameObject.activeInHierarchy,
                alpha = panel.alpha, interactive = panel.interactable && panel.blocksRaycasts
            }).ToArray();
            var state = new State
            {
                seq = _sequence, error = _error, scene = SceneManager.GetActiveScene().name,
                status = sessions != null ? sessions.Status : "",
                busy = _signingIn || sessions != null && sessions.IsBusy,
                matching = sessions != null && sessions.IsMatching,
                joined = sessions != null && sessions.IsInSession,
                host = sessions != null && sessions.IsHost,
                canStart = sessions != null && sessions.CanStart,
                room = sessions != null ? sessions.RoomName : "",
                code = sessions != null ? sessions.JoinCode : "",
                count = sessions != null ? sessions.PlayerCount : 0,
                roomId = sessions != null ? sessions.RoomId : "",
                profile = initialized ? AuthenticationService.Instance.Profile : "",
                signedIn = initialized && AuthenticationService.Instance.IsSignedIn,
                listening = network != null && network.IsListening,
                server = network != null && network.IsServer,
                sessions = initialized ? MultiplayerService.Instance.Sessions.Count : 0,
                protocol = NetGame.Protocol,
                config = network != null ? network.NetworkConfig.GetConfig(false).ToString("X16") : "",
                rooms = sessions != null ? sessions.Rooms.Select(room => new Room
                {
                    id = room.Id, name = room.Name, players = room.PlayerCount
                }).ToArray() : Array.Empty<Room>(),
                buttons = buttons, inputs = inputs, panels = panels
            };
            File.WriteAllText(_path + ".json", JsonUtility.ToJson(state, true));
        }
    }
}
#endif
