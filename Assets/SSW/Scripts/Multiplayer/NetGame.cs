using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSW
{
    public sealed class NetGame : MonoBehaviour
    {
        const string JobMessage = "mushrooms.job";
        const string GameScene = "SuperUltraLegendScene";
        [SerializeField] NetworkManager _managerPrefab;
        [SerializeField] NetPlayer _playerPrefab;
        [SerializeField] NetMatch _matchPrefab;
        [SerializeField] NetworkObject[] _shots;
        [SerializeField] MatchUI _menuPrefab;
        [SerializeField] Vs _introPrefab;
        readonly Dictionary<ulong, Fighter> _fighters = new Dictionary<ulong, Fighter>();
        Fighter _localInfo;
        Vs _intro;
        readonly Dictionary<ulong, PlayerJob> _jobs = new Dictionary<ulong, PlayerJob>();
        readonly List<NetPlayer> _players = new List<NetPlayer>();
        NetworkManager _manager;
        MatchUI _menu;
        PlayerJob _localJob;
        bool _explicitJob;
        ulong _localId;
        bool _bound;
        bool _messages;
        bool _loading;
        bool _sceneReady;
        bool _autoStart;
        bool _leaving;
        bool _finished;
        bool _inMatch;
        MatchState _result;

        public static NetGame Current { get; private set; }
        public NetworkManager Manager => _manager;
        public bool Connected => _manager != null && _manager.IsListening;
        public ulong LocalId => _localId;
        public bool Ready => Connected && _manager.IsServer && _manager.ConnectedClientsIds.Count == 2 && _jobs.Count == 2;
        public MatchState State => _finished ? _result : Match != null ? Match.State : default;
        internal MatchUI Menu => _menu;
        public float IntroDuration => _introPrefab.Duration;
        public Vs Intro => _intro;
        public NetArena Arena { get; private set; }
        public NetMatch Match { get; private set; }
        public IReadOnlyList<NetPlayer> Players => _players;
        public NetPlayer Local => _players.FirstOrDefault(player => player.IsOwner);
        public bool CanFight => Match != null && Match.Playing && !_leaving && !_finished;
        public bool HasPlayerPrefab => _playerPrefab != null;
        public event Action<MatchState> Ended;
        public event Action ConnectionChanged;

        public static NetGame GetOrCreate()
        {
            if (Current != null) return Current;
            return Instantiate(Resources.Load<NetGame>("Network/NetGame"));
        }

        void Awake()
        {
            Current = this;
            DontDestroyOnLoad(gameObject);
            _localJob = PlayerJobStorage.Load();
            _localInfo = Fighter.Create(NetLaunch.Name, NetLaunch.Tag);
        }

        public void Prepare()
        {
            if (_manager == null)
            {
                _bound = false;
                _manager = NetworkManager.Singleton != null
                    ? NetworkManager.Singleton
                    : Instantiate(_managerPrefab);
            }
            if (!_manager.IsListening)
            {
                if (!_explicitJob) _localJob = PlayerJobStorage.Load();
                if (!NetMath.Supported(_localJob)) throw new ArgumentException("온라인 직업은 마녀와 마술사 중에서 선택해 주세요.");
                _manager.NetworkConfig.PlayerPrefab = null;
                _manager.NetworkConfig.EnableSceneManagement = true;
                RegisterPrefab(_playerPrefab.gameObject);
                RegisterPrefab(_matchPrefab.gameObject);
                foreach (NetworkObject prefab in _shots) RegisterPrefab(prefab.gameObject);
                _jobs.Clear();
                _fighters.Clear();
                _players.Clear();
                _messages = false;
                _loading = false;
                _sceneReady = false;
                _leaving = false;
                _finished = false;
                _inMatch = false;
                Match = null;
            }
            if (_bound) return;
            _bound = true;
            _manager.OnServerStarted += Started;
            _manager.OnClientStarted += Started;
            _manager.OnClientConnectedCallback += Join;
            _manager.OnClientDisconnectCallback += Disconnected;
            _manager.OnClientStopped += Stopped;
            _manager.OnServerStopped += Stopped;
            if (_manager.IsListening) Started();
        }

        void RegisterPrefab(GameObject prefab)
        {
            if (_manager.NetworkConfig.Prefabs.Prefabs.Any(entry => entry.Prefab == prefab)) return;
            _manager.AddNetworkPrefab(prefab);
        }

        public void SetProfile(Fighter info)
        {
            _localInfo = info;
        }

        public void UseName(string name)
        {
            if (_localInfo.Name.IsEmpty) _localInfo = Fighter.Create(name, _localInfo.Tag.ToString());
        }

        public void SetLocalJob(PlayerJob job)
        {
            if (!NetMath.Supported(job)) throw new ArgumentException("온라인 직업은 마녀와 마술사 중에서 선택해 주세요.");
            _localJob = job;
            _explicitJob = true;
        }

        public void SetJob(ulong client, PlayerJob job)
        {
            if (!_manager.IsServer || _inMatch || !NetMath.Supported(job)) return;
            if (!_manager.ConnectedClients.ContainsKey(client)) return;
            _jobs[client] = job;
            ConnectionChanged?.Invoke();
            TryStart();
        }

        public void StartLocal(bool host, string address, PlayerJob job, ushort port = 7777)
        {
            SetLocalJob(job);
            Prepare();
            _autoStart = true;
            UnityTransport transport = (UnityTransport)_manager.NetworkConfig.NetworkTransport;
            transport.SetConnectionData(address, port, "0.0.0.0");
            bool started = host ? _manager.StartHost() : _manager.StartClient();
            if (!started) throw new InvalidOperationException("로컬 연결을 시작하지 못했습니다.");
        }

        void Started()
        {
            if (!_messages)
            {
                _manager.CustomMessagingManager.RegisterNamedMessageHandler(JobMessage, ReceiveJob);
                _messages = true;
            }
            if (_manager.IsServer)
            {
                _manager.SceneManager.OnLoadEventCompleted -= Loaded;
                _manager.SceneManager.OnLoadEventCompleted += Loaded;
            }
        }

        void Join(ulong client)
        {
            if (_manager.IsServer && (_manager.ConnectedClientsIds.Count > 2 || _inMatch))
            {
                if (client != NetworkManager.ServerClientId) _manager.DisconnectClient(client, "이미 대전 중입니다.");
                return;
            }
            if (client != _manager.LocalClientId) return;
            _localId = client;
            ConnectionChanged?.Invoke();
            PlayerJob job = NetMath.Supported(_localJob) ? _localJob : PlayerJob.Magician;
            _fighters[client] = _localInfo;
            if (_manager.IsServer) SetJob(client, job);
            else
            {
                using FastBufferWriter writer = new FastBufferWriter(512, Allocator.Temp);
                writer.WriteValueSafe((int)job);
                writer.WriteNetworkSerializable(_localInfo);
                _manager.CustomMessagingManager.SendNamedMessage(JobMessage, NetworkManager.ServerClientId, writer);
            }
        }

        void ReceiveJob(ulong client, FastBufferReader reader)
        {
            if (!_manager.IsServer || reader.Length - reader.Position < sizeof(int)) return;
            try
            {
                reader.ReadValueSafe(out int job);
                reader.ReadNetworkSerializable(out Fighter info);
                _fighters[client] = info;
                SetJob(client, (PlayerJob)job);
            }
            catch (Exception error) when (error is OverflowException || error is ArgumentException) { }
        }

        public void LoadScene(string sceneName)
        {
            Prepare();
            if (!Ready)
                throw new InvalidOperationException("두 플레이어가 연결되어야 시작할 수 있습니다.");
            if (_loading || _inMatch) return;
            _loading = true;
            _sceneReady = false;
            SceneEventProgressStatus status = _manager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return;
            _loading = false;
            throw new InvalidOperationException($"맵을 불러오지 못했습니다 ({status}).");
        }

        public void StartMatch()
        {
            if (!_manager.IsServer) return;
            if (Arena == null) LoadScene(GameScene);
            else
            {
                _sceneReady = true;
                TryStart();
            }
        }

        void Loaded(string scene, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (!_loading || Arena == null) return;
            _loading = false;
            if (timedOut.Count > 0 || completed.Count != 2)
            {
                ShowLost();
                return;
            }
            _sceneReady = true;
            TryStart();
        }

        void TryStart()
        {
            if (!_manager.IsServer || _inMatch || _jobs.Count != 2) return;
            if (!_sceneReady)
            {
                if (_autoStart && !_loading) LoadScene(GameScene);
                return;
            }
            _inMatch = true;
            NetMatch match = Instantiate(_matchPrefab);
            match.NetworkObject.Spawn(true);
            int slot = 0;
            foreach (ulong client in _manager.ConnectedClientsIds.OrderBy(id => id))
            {
                NetPlayer player = Instantiate(_playerPrefab, Arena.Spawn(slot), Quaternion.identity);
                _fighters.TryGetValue(client, out Fighter info);
                player.Init(_jobs[client], slot == 0 ? 1 : -1, info);
                slot++;
                player.NetworkObject.SpawnAsPlayerObject(client, true);
            }
            Physics2D.IgnoreCollision(_players[0].Collider, _players[1].Collider);
            match.Begin();
        }

        public void Enter(NetArena arena)
        {
            Arena = arena;
            _menu = Instantiate(_menuPrefab);
            _menu.Bind(this);
        }

        public void Leave(NetArena arena)
        {
            if (Arena == arena) Arena = null;
        }

        public void SetMatch(NetMatch match)
        {
            Match = match;
            _inMatch = true;
        }

        public void Register(NetPlayer player)
        {
            _players.Add(player);
            RefreshCamera();
            ShowIntro();
        }

        public void Unregister(NetPlayer player)
        {
            _players.Remove(player);
        }

        void RefreshCamera()
        {
            NetPlayer local = Local;
            if (local == null || Arena == null) return;
            NetPlayer opponent = _players.FirstOrDefault(player => !player.IsOwner);
            Arena.Follow(local.transform, opponent != null ? opponent.transform : local.transform, local.Side);
        }

        void ShowIntro()
        {
            if (_intro != null || Match == null || State.Phase != MatchPhase.Intro || _players.Count != 2) return;
            NetPlayer local = Local;
            if (local == null) return;
            NetPlayer opponent = _players[0] == local ? _players[1] : _players[0];
            _intro = Instantiate(_introPrefab);
            _intro.SetView(Arena.View);
            _intro.Show(local, opponent, () => Match.IntroShown());
        }

        void CloseIntro(bool animate)
        {
            if (_intro == null) return;
            if (animate) _intro.Close();
            else
            {
                Destroy(_intro.gameObject);
                _intro = null;
            }
        }

        public void MatchChanged(MatchState state)
        {
            if (state.Phase == MatchPhase.Intro) ShowIntro();
            if (state.Phase == MatchPhase.Draft) CloseIntro(true);
            if (state.Phase != MatchPhase.Finished || _finished) return;
            CloseIntro(false);
            _finished = true;
            _result = state;
            foreach (NetPlayer player in _players) player.Draft.Close();
            _menu.ShowResult(state);
            Ended?.Invoke(state);
        }

        void Disconnected(ulong client)
        {
            if (_leaving) return;
            _jobs.Remove(client);
            _fighters.Remove(client);
            ConnectionChanged?.Invoke();
            if (!_inMatch && !_loading && Arena == null) return;
            if (_manager.IsServer && Match != null) Match.Forfeit(client);
            else ShowLost();
        }

        void Stopped(bool wasHost)
        {
            ConnectionChanged?.Invoke();
            _messages = false;
            if (!_leaving && _inMatch && !_finished) ShowLost();
        }

        void ShowLost()
        {
            MatchState state = new MatchState
            {
                Phase = MatchPhase.Finished,
                Winner = _localId,
                Reason = MatchEnd.Left
            };
            if (_menu != null) MatchChanged(state);
        }

        public async void Exit()
        {
            if (_leaving) return;
            _leaving = true;
            CloseIntro(false);
            Time.timeScale = 1f;
            MultiplayerSessionManager session = MultiplayerSessionManager.Current;
            if (session != null && session.IsInSession)
            {
                try { await session.LeaveRoomAsync(); }
                catch (Exception exception) { Debug.LogWarning(exception.Message); }
            }
            _manager.Shutdown();
            StartCoroutine(Return());
        }

        IEnumerator Return()
        {
            while (_manager.ShutdownInProgress) yield return null;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            _autoStart = false;
            _explicitJob = false;
            _inMatch = false;
            _players.Clear();
            _jobs.Clear();
            Match = null;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (!_bound || _manager == null) return;
            _manager.OnServerStarted -= Started;
            _manager.OnClientStarted -= Started;
            _manager.OnClientConnectedCallback -= Join;
            _manager.OnClientDisconnectCallback -= Disconnected;
            _manager.OnClientStopped -= Stopped;
            _manager.OnServerStopped -= Stopped;
        }
    }
}