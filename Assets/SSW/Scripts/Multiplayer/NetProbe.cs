#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SSW
{
    public sealed class NetProbe : MonoBehaviour
    {
        [Serializable] sealed class Command
        {
            public int seq;
            public string op;
            public float x;
            public float y;
            public int value;
        }

        [Serializable] sealed class PlayerState
        {
            public ulong id;
            public string job;
            public string name;
            public bool draftView;
            public float hp;
            public float max;
            public Vector2 position;
            public Vector2 velocity;
            public Vector3 viewport;
            public Vector2 aim;
            public int side;
            public bool labelsFaceView;
            public int animation;
            public bool owner;
            public bool ready;
            public Vector3Int offer;
            public int[] augments;
            public int potion;
            public int rank;
        }

        [Serializable] sealed class ShotState
        {
            public ulong id;
            public string type;
            public Vector2 position;
            public int rank;
            public int kind;
        }

        [Serializable] sealed class Snapshot
        {
            public int seq;
            public string error;
            public bool listening;
            public bool server;
            public bool connected;
            public int peers;
            public bool readyToStart;
            public string scene;
            public bool menuOpen;
            public bool canResume;
            public string title;
            public string phase;
            public string reason;
            public ulong winner;
            public float timeScale;
            public float time;
            public bool intro;
            public bool introClosing;
            public string leftName;
            public string rightName;
            public PlayerState[] players;
            public ShotState[] shots;
        }

        bool _watch;
        bool _captured;
        float _introAt = -1f;
        string _path;
        int _sequence;
        float _next;
        string _error = "";

        public void Init(string path)
        {
            Application.targetFrameRate = 60;
            _path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            Application.logMessageReceived += Log;
        }

        void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _error = message;
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
            catch (Exception exception) { _error = exception.ToString(); }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void Execute(Command command)
        {
            NetGame game = NetGame.Current;
            NetPlayer local = game.Local;
            switch (command.op)
            {
                case "watch": _watch = true; _captured = false; _introAt = -1f; break;
                case "move": local.Move(new Vector2(command.x, command.y)); break;
                case "jump": local.Jump(); break;
                case "press": local.Cast.Attack(true, new Vector2(command.x, command.y)); break;
                case "release": local.Cast.Attack(false, new Vector2(command.x, command.y)); break;
                case "aim":
                    Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                    Vector3 point = game.Arena.View.ViewportToScreenPoint(new Vector3(command.x, command.y));
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                    break;
                case "mouse":
                    Mouse pointer = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                    InputSystem.QueueStateEvent(pointer, new MouseState
                    {
                        position = pointer.position.ReadValue(),
                        buttons = (ushort)command.value
                    });
                    break;
                case "fire": local.Cast.Attack(command.value > 0, local.Aim); break;
                case "cycle": local.Cast.Cycle(command.value > 0); break;
                case "choose": local.Draft.Choose(command.value); break;
                case "damage": local.Health.TakeDamage(command.value); break;
                case "heal": local.Health.Heal(command.value); break;
                case "remote":
                    foreach (NetPlayer player in game.Players)
                        if (!player.IsOwner) player.Health.TakeDamage(command.value);
                    break;
                case "capture": ScreenCapture.CaptureScreenshot(_path + ".png"); break;
                case "escape": StartCoroutine(Escape()); break;
                case "resume": game.Menu.Resume(); break;
                case "exit": game.Menu.Exit(); break;
                case "start":
                    game.StartLocal(command.value > 0, "127.0.0.1",
                        command.x < 0.5f ? PlayerJob.Witch : PlayerJob.Magician, (ushort)command.y);
                    break;
                case "quit": Application.Quit(); break;
            }
        }

        IEnumerator Escape()
        {
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        void Write()
        {
            NetGame game = NetGame.Current;
            if (_watch && game.Intro != null && !game.Intro.IsClosing)
            {
                if (_introAt < 0f) _introAt = Time.unscaledTime;
                if (!_captured && Time.unscaledTime - _introAt >= 0.85f)
                {
                    _captured = true;
                    ScreenCapture.CaptureScreenshot(_path + ".intro.png");
                }
            }
            List<PlayerState> players = new List<PlayerState>();
            List<ShotState> shots = new List<ShotState>();
            foreach (NetPlayer player in game.Players)
            {
                List<int> owned = new List<int>(player.Draft.Owned);
                Animator animator = player.GetComponentInChildren<Animator>();
                players.Add(new PlayerState
                {
                    id = player.OwnerClientId, job = player.Job.ToString(), hp = player.Health.Current,
                    name = player.Info.Name.ToString(), draftView = player.Draft.HasView,
                    max = player.Health.Max, position = player.transform.position, velocity = player.Body.linearVelocity,
                    viewport = game.Arena.View.WorldToViewportPoint(player.transform.position),
                    aim = player.Aim, side = player.Side, labelsFaceView = LabelsFaceView(player, game.Arena.View),
                    animation = animator.GetCurrentAnimatorStateInfo(0).shortNameHash, owner = player.IsOwner,
                    ready = player.Draft.Ready, offer = player.IsOwner ? player.Draft.Offer : default,
                    augments = owned.ToArray(), potion = player.Cast.Held, rank = player.Cast.Rank
                });
            }
            if (game.Connected)
                foreach (var entry in game.Manager.SpawnManager.SpawnedObjects)
                {
                    var obj = entry.Value;
                    if (obj.TryGetComponent(out NetCard card))
                        shots.Add(new ShotState { id = entry.Key, type = "card", position = obj.transform.position, rank = card.State.Rank });
                    else if (obj.TryGetComponent(out NetPotion potion))
                        shots.Add(new ShotState { id = entry.Key, type = "potion", position = obj.transform.position, kind = potion.Kind });
                    else if (obj.TryGetComponent(out NetZone zone))
                        shots.Add(new ShotState { id = entry.Key, type = "zone", position = obj.transform.position });
                }
            MatchState state = game.State;
            Snapshot snapshot = new Snapshot
            {
                seq = _sequence, error = _error, listening = game.Connected,
                server = game.Connected && game.Manager.IsServer, connected = game.Connected && game.Manager.IsConnectedClient,
                peers = game.Connected && game.Manager.IsServer ? game.Manager.ConnectedClientsIds.Count : 0,
                readyToStart = game.Ready, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                menuOpen = game.Menu != null && game.Menu.IsOpen,
                canResume = game.Menu != null && game.Menu.CanResume,
                title = game.Menu != null ? game.Menu.Title : string.Empty,
                phase = state.Phase.ToString(), reason = state.Reason.ToString(),
                time = Time.unscaledTime, intro = game.Intro != null,
                introClosing = game.Intro != null && game.Intro.IsClosing,
                leftName = game.Intro != null ? game.Intro.LeftName : "",
                rightName = game.Intro != null ? game.Intro.RightName : "",
                winner = state.Winner, timeScale = Time.timeScale, players = players.ToArray(), shots = shots.ToArray()
            };
            File.WriteAllText(_path + ".json", JsonUtility.ToJson(snapshot, true));
        }

        static bool LabelsFaceView(NetPlayer player, Camera view)
        {
            foreach (NetLabel label in player.GetComponentsInChildren<NetLabel>(true))
                if (Vector3.Dot(label.transform.right, view.transform.right) < 0.99f) return false;
            return true;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }
    }
}
#endif
