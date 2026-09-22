#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            public Control[] buttons;
            public Control[] inputs;
            public Panel[] panels;
        }

        string _path;
        int _sequence;
        float _next;
        string _error = "";
        bool _drive;

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
            }
        }

        void Write()
        {
            var sessions = MultiplayerSessionManager.Current;
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
                busy = sessions != null && sessions.IsBusy,
                matching = sessions != null && sessions.IsMatching,
                joined = sessions != null && sessions.IsInSession,
                host = sessions != null && sessions.IsHost,
                canStart = sessions != null && sessions.CanStart,
                room = sessions != null ? sessions.RoomName : "",
                code = sessions != null ? sessions.JoinCode : "",
                count = sessions != null ? sessions.PlayerCount : 0,
                buttons = buttons, inputs = inputs, panels = panels
            };
            File.WriteAllText(_path + ".json", JsonUtility.ToJson(state, true));
        }
    }
}
#endif
