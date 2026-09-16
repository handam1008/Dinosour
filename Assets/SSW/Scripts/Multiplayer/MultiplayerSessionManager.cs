using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerSessionManager : MonoBehaviour
    {
        const string SessionType = "mushrooms-1v1-session";
        const string GameProperty = "game";
        const string GamePropertyValue = "mushrooms-1v1-v1";
        const string ModeProperty = "mode";
        const int PlayerLimit = 2;
        static MultiplayerSessionManager _instance;
        readonly List<MultiplayerRoomInfo> _rooms = new List<MultiplayerRoomInfo>();
        NetGame _network;
        Task _initializeTask;
        Task _operation = Task.CompletedTask;
        ISession _session;
        bool _busy;
        bool _cancelled;
        bool _matching;
        bool _starting;
        string _status = string.Empty;

        public static MultiplayerSessionManager Current => _instance;
        public event Action Changed;
        public IReadOnlyList<MultiplayerRoomInfo> Rooms => _rooms;
        public bool IsBusy => _busy;
        public bool IsMatching => _matching;
        public bool IsInSession => _session != null;
        public bool IsHost => _session != null && _session.IsHost;
        public bool CanStart => IsHost && _network.Ready && !_busy && !_starting;
        public string Status => _status;
        public string RoomName => _session != null ? _session.Name : string.Empty;
        public string JoinCode => _session != null ? _session.Code : string.Empty;
        public int PlayerCount => _session != null ? _session.PlayerCount : 0;
        public bool HasNetworkPlayer => _network != null && _network.HasPlayerPrefab;

        public static MultiplayerSessionManager GetOrCreate()
        {
            if (_instance != null) return _instance;
            return new GameObject("MultiplayerSession").AddComponent<MultiplayerSessionManager>();
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _network = NetGame.GetOrCreate();
            _network.ConnectionChanged += NotifyChanged;
        }

        public Task RefreshRoomsAsync()
        {
            return RunAsync(async () =>
            {
                SetStatus("방 목록을 불러오는 중...");
                await EnsureInitializedAsync();
                CheckCancel();
                QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(Query("room"));
                CheckCancel();
                _rooms.Clear();
                foreach (ISessionInfo room in result.Sessions)
                    _rooms.Add(new MultiplayerRoomInfo(room.Id, room.Name, room.MaxPlayers - room.AvailableSlots, room.HasPassword));
                SetStatus(_rooms.Count == 0 ? "현재 참가할 수 있는 방이 없습니다" : string.Empty);
            });
        }

        public Task CreateRoomAsync(MultiplayerRoomRequest request)
        {
            return RunAsync(async () =>
            {
                ValidatePassword(request.Password);
                SetStatus("방을 만드는 중...");
                await EnsureInitializedAsync();
                CheckCancel();
                _network.Prepare();
                string name = string.IsNullOrWhiteSpace(request.Name) ? "새로운 방" : request.Name.Trim();
                var session = await MultiplayerService.Instance.CreateSessionAsync(
                    CreateOptions(name, request.Password, request.HiddenFromList, "room"));
                await Accept(session);
                SetStatus(string.Empty);
            });
        }

        public Task JoinRoomAsync(string roomId, string password = null)
        {
            return RunAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("참가할 방을 선택해주세요");
                SetStatus("방에 참가하는 중...");
                await EnsureInitializedAsync();
                CheckCancel();
                _network.Prepare();
                var session = await MultiplayerService.Instance.JoinSessionByIdAsync(roomId, JoinOptions(password));
                await Accept(session);
                SetStatus(string.Empty);
            });
        }

        public Task JoinByCodeAsync(string joinCode, string password = null)
        {
            return RunAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(joinCode)) throw new ArgumentException("참가 코드를 입력해주세요");
                SetStatus("참가 코드로 연결하는 중...");
                await EnsureInitializedAsync();
                CheckCancel();
                _network.Prepare();
                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                    joinCode.Trim().ToUpperInvariant(), JoinOptions(password));
                await Accept(session);
                SetStatus(string.Empty);
            });
        }

        public Task QuickPlayAsync(string sceneName = "SuperUltraLegendScene")
        {
            return RunAsync(async () =>
            {
                _matching = true;
                SetStatus("상대를 찾는 중...");
                try
                {
                    await EnsureInitializedAsync();
                    double nextSearch = 0;
                    while (_network.Arena == null)
                    {
                        CheckCancel();
                        if (_session == null) await FindMatch();
                        if (IsHost && _network.Ready)
                        {
                            await BeginGame(sceneName);
                            return;
                        }
                        if (IsHost && PlayerCount == 1 && Time.unscaledTimeAsDouble >= nextSearch)
                        {
                            nextSearch = Time.unscaledTimeAsDouble + 2.5;
                            await MergeMatch();
                        }
                        await Task.Delay(100);
                    }
                    CheckCancel();
                }
                catch
                {
                    await Drop();
                    throw;
                }
                finally
                {
                    _matching = false;
                    NotifyChanged();
                }
            });
        }

        async Task FindMatch()
        {
            CheckCancel();
            _network.Prepare();
            var options = new QuickJoinOptions
            {
                CreateSession = true,
                Filters = Filters("quick")
            };
            options.Filters.Add(new FilterOption(FilterField.HasPassword, "false", FilterOperation.Equal));
            ISession session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                options, CreateOptions("빠른 대전", null, false, "quick"));
            await Accept(session);
        }

        async Task MergeMatch()
        {
            ISession waiting = _session;
            QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(Query("quick"));
            CheckCancel();
            if (_session != waiting || PlayerCount > 1 || _network.Manager.ConnectedClientsIds.Count > 1) return;
            ISessionInfo target = result.Sessions
                .Where(room => string.CompareOrdinal(room.Id, waiting.Id) < 0 && !room.HasPassword)
                .OrderBy(room => room.Id, StringComparer.Ordinal).FirstOrDefault();
            if (target == null) return;
            await Drop();
            CheckCancel();
            _network.Prepare();
            try
            {
                await Accept(await MultiplayerService.Instance.JoinSessionByIdAsync(target.Id, JoinOptions(null)));
            }
            catch (SessionException exception) when (
                exception.Error == SessionError.SessionNotFound ||
                exception.Error == SessionError.SessionDeleted ||
                exception.Error == SessionError.Forbidden ||
                exception.Error == SessionError.SessionConflict)
            {
                SetStatus("상대를 찾는 중...");
            }
        }

        public Task LeaveRoomAsync()
        {
            return RunAsync(async () =>
            {
                SetStatus("방에서 나가는 중...");
                await Drop();
                SetStatus(string.Empty);
            });
        }

        public async Task CancelAsync()
        {
            _cancelled = true;
            if (_busy)
            {
                SetStatus("취소하는 중...");
                try { await _operation; }
                catch (Exception) { }
            }
            if (_session != null) await LeaveRoomAsync();
            SetStatus(string.Empty);
        }

        public Task StartGameAsync(string sceneName)
        {
            return RunAsync(async () =>
            {
                if (!IsHost) throw new InvalidOperationException("방장만 게임을 시작할 수 있습니다");
                if (_starting) return;
                await BeginGame(sceneName);
            });
        }

        async Task BeginGame(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) throw new ArgumentException("시작할 맵이 없습니다");
            if (!_network.Ready) throw new InvalidOperationException("상대의 연결과 직업 선택을 기다려 주세요.");
            _starting = true;
            try
            {
                SetStatus("대전을 시작하는 중...");
                IHostSession host = _session.AsHost();
                host.IsLocked = true;
                await host.SavePropertiesAsync();
                CheckCancel();
                _network.LoadScene(sceneName);
            }
            catch
            {
                _starting = false;
                throw;
            }
        }

        async Task Accept(ISession session)
        {
            if (_cancelled)
            {
                await session.LeaveAsync();
                CheckCancel();
            }
            SetSession(session);
            if (_matching) SetStatus("상대를 찾는 중...");
        }

        async Task Drop()
        {
            if (_session == null) return;
            ISession leaving = _session;
            await leaving.LeaveAsync();
            SetSession(null);
        }

        async Task EnsureInitializedAsync()
        {
            if (_initializeTask == null) _initializeTask = InitializeServicesAsync();
            try
            {
                await _initializeTask;
                _network.UseName(AuthenticationService.Instance.PlayerName);
            }
            catch
            {
                _initializeTask = null;
                throw;
            }
        }

        static async Task InitializeServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(NetLaunch.Profile));
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Task RunAsync(Func<Task> operation)
        {
            if (_busy) return Task.FromException(new InvalidOperationException("이전 작업이 끝날 때까지 잠시 기다려주세요"));
            _busy = true;
            _cancelled = false;
            Changed?.Invoke();
            _operation = ExecuteAsync(operation);
            return _operation;
        }

        async Task ExecuteAsync(Func<Task> operation)
        {
            try { await operation(); }
            catch (OperationCanceledException)
            {
                SetStatus(string.Empty);
                throw;
            }
            catch (Exception exception)
            {
                SetStatus(ToReadableMessage(exception));
                throw;
            }
            finally
            {
                _busy = false;
                Changed?.Invoke();
            }
        }

        void CheckCancel()
        {
            if (_cancelled) throw new OperationCanceledException();
        }

        static SessionOptions CreateOptions(string name, string password, bool isPrivate, string mode)
        {
            return new SessionOptions
            {
                Type = SessionType,
                Name = name,
                MaxPlayers = PlayerLimit,
                Password = EmptyToNull(password),
                IsPrivate = isPrivate,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { GameProperty, new SessionProperty(GamePropertyValue, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                    { ModeProperty, new SessionProperty(mode, VisibilityPropertyOptions.Public, PropertyIndex.String2) }
                }
            }.WithRelayNetwork();
        }

        static JoinSessionOptions JoinOptions(string password)
        {
            return new JoinSessionOptions { Type = SessionType, Password = EmptyToNull(password) };
        }

        static List<FilterOption> Filters(string mode)
        {
            return new List<FilterOption>
            {
                new FilterOption(FilterField.StringIndex1, GamePropertyValue, FilterOperation.Equal),
                new FilterOption(FilterField.StringIndex2, mode, FilterOperation.Equal),
                new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)
            };
        }

        static QuerySessionsOptions Query(string mode)
        {
            return new QuerySessionsOptions
            {
                Count = 30,
                FilterOptions = Filters(mode),
                SortOptions = new List<SortOption> { new SortOption(SortOrder.Ascending, SortField.CreationTime) }
            };
        }

        void SetSession(ISession session)
        {
            if (_session != null)
            {
                _session.Changed -= NotifyChanged;
                _session.RemovedFromSession -= HandleSessionEnded;
                _session.Deleted -= HandleSessionEnded;
            }
            _session = session;
            _starting = false;
            if (_session != null)
            {
                _session.Changed += NotifyChanged;
                _session.RemovedFromSession += HandleSessionEnded;
                _session.Deleted += HandleSessionEnded;
            }
            Changed?.Invoke();
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
        }

        void HandleSessionEnded()
        {
            SetSession(null);
            SetStatus("방 연결이 종료되었습니다");
        }

        void SetStatus(string value)
        {
            _status = value;
            Changed?.Invoke();
        }

        static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return;
            int length = password.Trim().Length;
            if (length < 8 || length > 64) throw new ArgumentException("비밀번호는 8자 이상 64자 이하로 입력해주세요");
        }

        void OnDestroy()
        {
            if (_instance != this) return;
            _cancelled = true;
            _network.ConnectionChanged -= NotifyChanged;
            _instance = null;
        }

        public static string ToReadableMessage(Exception exception)
        {
            if (exception is ArgumentException || exception is InvalidOperationException)
                return exception.Message;

            if (exception is SessionException sessionException)
            {
                switch (sessionException.Error)
                {
                    case SessionError.Unknown when sessionException.Message.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0:
                        return "비밀번호를 확인해주세요";
                    case SessionError.SessionNotFound:
                    case SessionError.SessionDeleted:
                        return "방이 사라졌거나 이미 게임이 시작되었습니다";
                    case SessionError.Forbidden:
                    case SessionError.NotAuthorized:
                        return "비밀번호가 틀렸거나 이 방에 참가할 수 없습니다";
                    case SessionError.RateLimitExceeded:
                        return "요청이 너무 많습니다. 잠시 후 다시 시도해주세요";
                    case SessionError.NetworkManagerNotInitialized:
                    case SessionError.NetworkManagerStartFailed:
                    case SessionError.NetworkSetupFailed:
                    case SessionError.TransportComponentMissing:
                        return "네트워크 연결을 시작하지 못했습니다";
                }
            }

            if (exception is AuthenticationException)
                return "Unity 로그인에 실패했습니다. 인터넷 연결과 Services 설정을 확인해주세요";

            if (exception is RequestFailedException requestFailed)
                return $"Unity 서비스 요청에 실패했습니다 ({requestFailed.ErrorCode})";

            return "멀티플레이 연결 중 오류가 발생했습니다";
        }
    }
}
