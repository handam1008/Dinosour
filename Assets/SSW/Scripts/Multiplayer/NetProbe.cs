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
    [DefaultExecutionOrder(500)]
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
            public bool spectating;
            public bool portraitRight;
            public int hover;
            public Vector2 cursor;
            public int trail;
            public Vector3Int watchOffer;
            public int watchPick;
            public float hp;
            public float max;
            public Vector2 position;
            public Vector2 viewPosition;
            public bool viewLinked;
            public Vector2 velocity;
            public ulong objectId;
            public uint epoch;
            public uint tick;
            public uint processed;
            public uint buffered;
            public int resyncs;
            public float speed;
            public float silence;
            public float ackSilence;
            public bool grounded;
            public float correction;
            public float maxCorrection;
            public Vector3 viewport;
            public Vector2 aim;
            public int side;
            public bool labelsFaceView;
            public int animation;
            public bool owner;
            public bool ready;
            public bool draftUnlocked;
            public Vector3Int offer;
            public int[] augments;
            public int potion;
            public int rank;
            public int heldView;
            public int previews;
            public int visiblePreviews;
            public int matches;
            public int rejected;
            public Vector2 shotOrigin;
            public uint shotTick;
            public int shotKind;
            public double shotLag;
            public int fired;
            public bool charging;
            public uint action;
            public uint confirmed;
            public double readyIn;
            public int ammo;
            public int progress;
            public bool hidden;
            public bool shrunk;
            public bool blind;
            public bool immune;
            public float damageScale;
            public float size;
            public bool parry;
        }

        [Serializable] sealed class ShotState
        {
            public ulong id;
            public string type;
            public Vector2 position;

            public int rank;
            public int kind;
            public uint action;
            public ulong caster;
        }

        [Serializable] sealed class Snapshot
        {
            public SoundProbe.State audio;
            public int probeVersion;
            public string build;
            public int targetFps;
            public float frameTime;
            public ulong rtt;
            public double physicsTime;
            public double serverTime;
            public float viewDelay;
            public float bodyDelay;
            public float castDelay;
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
            public bool draftStatus;
            public string draftLabel;
            public bool wipe;
            public string wipeTitle;
            public string reason;
            public ulong winner;
            public byte round;
            public byte firstWins;
            public byte secondWins;
            public ulong first;
            public ulong second;
            public byte set;
            public byte firstSets;
            public byte secondSets;
            public byte firstMarks;
            public byte secondMarks;
            public float timeScale;
            public float time;
            public bool intro;
            public bool introClosing;
            public string leftName;
            public string rightName;
            public PlayerState[] players;
            public ShotState[] shots;
        }

        SoundProbe _audio;
        readonly NetTrace _trace = new NetTrace();
        float _measureAt;
        float _viewDelay = -1f;
        float _bodyDelay = -1f;
        double _castAt;
        float _castDelay = -1f;
        bool _measuringCast;
        Vector2 _viewStart;
        Vector2 _bodyStart;
        bool _measuring;
        bool _measureJump;
        bool _watch;
        bool _captured;
        float _introAt = -1f;
        string _path;
        int _sequence;
        float _next;
        string _error = "";
        bool _drive;

        public void Init(string path, bool drive = true)
        {
            _audio = gameObject.AddComponent<SoundProbe>();
            _drive = drive;
            if (drive) Application.targetFrameRate = 60;
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
            if (_measuring && NetGame.Current.Local != null)
            {
                NetPlayer player = NetGame.Current.Local;
                Vector2 viewDelta = (Vector2)player.View.position - _viewStart;
                Vector2 bodyDelta = player.Body.position - _bodyStart;
                if (_viewDelay < 0f && (_measureJump ? viewDelta.y > 0.035f : Mathf.Abs(viewDelta.x) > 0.035f)) _viewDelay = Time.unscaledTime - _measureAt;
                if (_bodyDelay < 0f && (_measureJump ? bodyDelta.y > 0.035f : Mathf.Abs(bodyDelta.x) > 0.035f)) _bodyDelay = Time.unscaledTime - _measureAt;
                if (_viewDelay >= 0f && _bodyDelay >= 0f) _measuring = false;
            }
            if (_measuringCast && NetGame.Current.Local != null && NetGame.Current.Local.Cast.FeedbackAt >= _castAt)
            {
                _castDelay = (float)(NetGame.Current.Local.Cast.FeedbackAt - _castAt);
                _measuringCast = false;
            }
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
            if (_drive) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void LateUpdate() => _trace.Sample(NetGame.Current);

        void Execute(Command command)
        {
            NetGame game = NetGame.Current;
            NetPlayer local = game.Local;
            switch (command.op)
            {
                case "sound": _audio.Execute(command.value, command.x, command.y); break;
                case "trace": _trace.Begin(_path + ".trace." + command.value + ".json", command.x); break;
                case "watch": _watch = true; _captured = false; _introAt = -1f; break;
                case "lag":
                    var transport = (Unity.Netcode.Transports.UTP.UnityTransport)game.Manager.NetworkConfig.NetworkTransport;
                    Unity.Networking.Transport.NetworkSimulatorParameterExtensions.ModifyNetworkSimulatorParameters(transport.GetNetworkDriver(), new Unity.Networking.Transport.NetworkSimulatorParameter
                    {
                        SendDelayMS = (uint)command.value, SendJitterMS = (uint)command.x, SendPacketLossPercent = command.y
                    });
                    break;
                case "measure":
                    _viewStart = local.View.position;
                    _bodyStart = local.Body.position;
                    _measureAt = Time.unscaledTime;
                    _viewDelay = _bodyDelay = -1f;
                    _measuring = true;
                    _measureJump = command.value == 1;
                    if (command.value == 1) local.Jump();
                    else local.Move(new Vector2(command.x, command.y));
                    break;
                case "push":
                    foreach (NetPlayer player in game.Players)
                        if (!player.IsOwner) player.GetComponent<PlayerController>().ApplyForce(new Vector2(command.x,command.y),ForceMode2D.Impulse);
                    break;
                case "slow":
                    foreach (NetPlayer player in game.Players)
                        if (!player.IsOwner) player.GetComponent<PlayerController>().ApplySlow(command.x,command.y);
                    break;
                case "haste":
                    foreach (NetPlayer player in game.Players)
                        if (!player.IsOwner) player.GetComponent<PlayerController>().ApplySpeed(command.x,command.y);
                    break;
                case "grant":
                    if (!game.Manager.IsServer) break;
                    foreach (NetPlayer player in game.Players)
                        if (player.IsOwner == (command.x < 0.5f)) player.Draft.Restore(new[] { command.value });
                    break;
                case "progress":
                    if (game.Manager.IsServer)
                        foreach (NetPlayer player in game.Players)
                            if (player.IsOwner == (command.x < 0.5f) && player.Cast.Weapon != null) player.Cast.Weapon.Progress = command.value;
                    break;
                case "seed": if (game.Manager.IsServer) UnityEngine.Random.InitState(command.value); break;
                case "metrics":
                    foreach (NetPlayer player in game.Players) player.GetComponent<MotionView>().ClearMetrics();
                    break;
                case "warp":
                    foreach (NetPlayer player in game.Players)
                        if (player.IsOwner == (command.value == 0)) player.GetComponent<MotionView>().Teleport(new Vector2(command.x, command.y));
                    break;
                case "stall": System.Threading.Thread.Sleep(Mathf.Clamp(command.value, 0, 1000)); break;
                case "fps":
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = Mathf.Clamp(command.value, 20, 240);
                    break;
                case "move": local.Move(new Vector2(command.x, command.y)); break;
                case "jump": local.Jump(); break;
                case "cast":
                    _castAt = Time.unscaledTimeAsDouble;
                    _castDelay = -1f;
                    _measuringCast = true;
                    local.Cast.Attack(command.value > 0, new Vector2(command.x, command.y));
                    break;
                case "cancel": local.Cast.Cancel(); break;
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
                case "cycle": local.Cast.Cycle(command.value > 0, command.x == 0f && command.y == 0f ? local.Aim : new Vector2(command.x, command.y)); break;
                case "parry": local.Cast.Parry(); break;
                case "choose": local.Draft.Choose(command.value); break;
                case "draftclick":
                    if (local.Draft.View != null)
                        local.Draft.View.GetComponentsInChildren<AugmentCardUI>(true)[command.value]
                            .OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                    break;
                case "watchclick":
                    foreach (NetPlayer player in game.Players)
                        if (!player.IsOwner && player.Draft.View != null)
                        {
                            player.Draft.View.GetComponentsInChildren<AugmentCardUI>(true)[command.value]
                                .OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                            player.Draft.Choose(command.value);
                        }
                    break;
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
                    ammo = player.Cast.Weapon != null ? player.Cast.Weapon.Status.Ammo : 0,
                    progress = player.Cast.Weapon != null ? player.Cast.Weapon.Progress : 0,
                    hidden = player.Effects.Hidden, shrunk = player.Effects.Shrunk,
                    blind = player.Effects.BlindActive, immune = player.Effects.ImmuneActive,
                    damageScale = player.Cast.Weapon != null ? player.Cast.Weapon.DamageScale : 1f,
                    size = player.Drive.Scale, parry = player.Cast.Weapon is SwordCast sword && sword.Parrying,
                    name = player.Info.Name.ToString(), draftView = player.Draft.HasView,
                    spectating = player.Draft.View != null && player.Draft.View.Spectating,
                    portraitRight = player.Draft.View != null && player.Draft.View.PortraitOnRight,
                    hover = player.Draft.View != null ? player.Draft.View.Hover : -1,
                    cursor = player.Draft.View != null ? player.Draft.View.Cursor : -Vector2.one,
                    trail = player.Draft.View != null ? player.Draft.View.Particles : 0,
                    watchOffer = player.Draft.WatchPose.Offer, watchPick = player.Draft.WatchPose.Pick,
                    max = player.Health.Max, position = player.transform.position, viewPosition = player.View.position, viewLinked = player.GetComponentInChildren<DinosaurVisualController>().transform.IsChildOf(player.View), velocity = player.Velocity,
                    objectId = player.NetworkObjectId, epoch = player.GetComponent<MotionView>().Epoch,
                    tick = player.GetComponent<MotionView>().Tick, processed = player.InputSequence,
                    grounded = player.GetComponent<MotionView>().Grounded, correction = player.GetComponent<MotionView>().Correction,
                    maxCorrection = player.GetComponent<MotionView>().MaxCorrection,
                    buffered = player.GetComponent<MotionView>().Buffered, resyncs = player.GetComponent<MotionView>().Resyncs,
                    speed = player.GetComponent<MotionView>().Speed, silence = player.GetComponent<MotionView>().Silence,
                    ackSilence = player.GetComponent<MotionView>().AckSilence,
                    viewport = game.Arena.View.WorldToViewportPoint(player.transform.position),
                    aim = player.Aim, side = player.Side, labelsFaceView = LabelsFaceView(player, game.Arena.View),
                    animation = animator.GetCurrentAnimatorStateInfo(0).shortNameHash, owner = player.IsOwner,
                    ready = player.Draft.Ready,
                    draftUnlocked = player.Draft.View != null && System.Array.TrueForAll(player.Draft.View.GetComponentsInChildren<AugmentCardUI>(true), card => !card.Locked),
                    offer = player.IsOwner ? player.Draft.Offer : default,
                    augments = owned.ToArray(), potion = player.Cast.Held, rank = player.Cast.Rank,
                    heldView = player.Cast.DisplayHeld, previews = player.Cast.Previews,
                    visiblePreviews = player.Cast.VisiblePreviews, matches = player.Cast.Matches,
                    shotOrigin = player.Cast.Origin, shotTick = player.Cast.ShotTick, shotKind = player.Cast.ShotKind, shotLag = player.Cast.ShotLag, fired = player.Cast.Shots,
                    rejected = player.Cast.Rejections, charging = player.Cast.Charging,
                    action = player.Cast.Action, confirmed = player.Cast.Confirmed, readyIn = player.Cast.ReadyIn
                });
            }
            if (game.Connected)
                foreach (var entry in game.Manager.SpawnManager.SpawnedObjects)
                {
                    var obj = entry.Value;
                    if (obj.TryGetComponent(out NetCard card))
                        shots.Add(new ShotState { id = entry.Key, type = "card", position = obj.transform.position, rank = card.State.Rank, action = card.State.Action, caster = card.State.Caster });
                    else if (obj.TryGetComponent(out NetPotion potion))
                        shots.Add(new ShotState { id = entry.Key, type = "potion", position = obj.transform.position, kind = potion.Kind, action = potion.Action, caster = potion.Caster });
                    else if (obj.TryGetComponent(out NetBolt bolt))
                        shots.Add(new ShotState { id = entry.Key, type = "bolt", position = obj.transform.position, kind = bolt.Spec.Style, action = bolt.Action, caster = bolt.Caster });
                    else if (obj.TryGetComponent(out NetZone zone))
                        shots.Add(new ShotState { id = entry.Key, type = "zone", position = obj.transform.position });
                }
            MatchState state = game.State;
            Snapshot snapshot = new Snapshot
            {
                physicsTime = game.PhysicsTime, serverTime = game.Connected ? game.Manager.ServerTime.Time : 0d,
                audio = _audio.Read(), probeVersion = 4, build = Application.buildGUID,
                rtt = game.Connected ? game.Manager.NetworkConfig.NetworkTransport.GetCurrentRtt(Unity.Netcode.NetworkManager.ServerClientId) : 0, targetFps = Application.targetFrameRate, frameTime = Time.unscaledDeltaTime,
                seq = _sequence, viewDelay = _viewDelay, bodyDelay = _bodyDelay, castDelay = _castDelay, error = _error, listening = game.Connected,
                server = game.Connected && game.Manager.IsServer, connected = game.Connected && game.Manager.IsConnectedClient,
                peers = game.Connected && game.Manager.IsServer ? game.Manager.ConnectedClientsIds.Count : 0,
                readyToStart = game.Ready, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                menuOpen = game.Menu != null && game.Menu.IsOpen,
                canResume = game.Menu != null && game.Menu.CanResume,
                title = game.Menu != null ? game.Menu.Title : string.Empty,
                phase = state.Phase.ToString(), reason = state.Reason.ToString(),
                draftStatus = game.Menu != null && game.Menu.Draft.Visible,
                draftLabel = game.Menu != null ? game.Menu.Draft.Label : "",
                wipe = game.Menu != null && game.Menu.Wipe.Visible,
                wipeTitle = game.Menu != null ? game.Menu.Wipe.Title : "",
                time = Time.unscaledTime, intro = game.Intro != null,
                introClosing = game.Intro != null && game.Intro.IsClosing,
                leftName = game.Intro != null ? game.Intro.LeftName : "",
                rightName = game.Intro != null ? game.Intro.RightName : "",
                winner = state.Winner, timeScale = Time.timeScale, players = players.ToArray(), shots = shots.ToArray(),
                round = state.Round, firstWins = state.FirstWins, secondWins = state.SecondWins,
                first = state.First, second = state.Second, set = state.Set,
                firstSets = state.FirstSets, secondSets = state.SecondSets,
                firstMarks = state.FirstMarks, secondMarks = state.SecondMarks
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
