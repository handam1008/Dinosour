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
        const int PlayerLimit = 2;

        static MultiplayerSessionManager _instance;

        readonly List<MultiplayerRoomInfo> _rooms = new List<MultiplayerRoomInfo>();
        MultiplayerNetworkRuntime _network;
        Task _initializeTask;
        ISession _session;
        bool _busy;
        string _status = string.Empty;

        public static MultiplayerSessionManager GetOrCreate()
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<MultiplayerSessionManager>();
            if (_instance != null) return _instance;

            GameObject managerObject = new GameObject("MultiplayerSession");
            return managerObject.AddComponent<MultiplayerSessionManager>();
        }

        public event Action Changed;

        public IReadOnlyList<MultiplayerRoomInfo> Rooms => _rooms;
        public bool IsBusy => _busy;
        public bool IsInSession => _session != null;
        public bool IsHost => _session != null && _session.IsHost;
        public string Status => _status;
        public string RoomName => _session != null ? _session.Name : string.Empty;
        public string JoinCode => _session != null ? _session.Code : string.Empty;
        public int PlayerCount => _session != null ? _session.PlayerCount : 0;
        public bool HasNetworkPlayer => _network != null && _network.HasPlayerPrefab;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _network = gameObject.AddComponent<MultiplayerNetworkRuntime>();
        }

        public async Task RefreshRoomsAsync()
        {
            await RunAsync(async () =>
            {
                await EnsureInitializedAsync();
                SetStatus("방 목록을 불러오는 중...");

                QuerySessionsOptions options = new QuerySessionsOptions
                {
                    Count = 30,
                    FilterOptions = new List<FilterOption>
                    {
                        GameFilter(),
                        new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                        new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)
                    },
                    SortOptions = new List<SortOption>
                    {
                        new SortOption(SortOrder.Descending, SortField.LastUpdated)
                    }
                };

                QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(options);
                _rooms.Clear();
                foreach (ISessionInfo room in result.Sessions)
                {
                    _rooms.Add(new MultiplayerRoomInfo(
                        room.Id,
                        string.IsNullOrWhiteSpace(room.Name) ? "이름 없는 방" : room.Name,
                        room.MaxPlayers - room.AvailableSlots,
                        room.HasPassword));
                }

                SetStatus(_rooms.Count == 0 ? "현재 참가할 수 있는 방이 없습니다" : $"방 {_rooms.Count}개를 찾았습니다");
            });
        }

        public async Task CreateRoomAsync(MultiplayerRoomRequest request)
        {
            await RunAsync(async () =>
            {
                await EnsureInitializedAsync();
                ValidatePassword(request.Password);
                _network.Prepare();
                SetStatus("방을 만드는 중...");

                SessionOptions options = CreateOptions(
                    string.IsNullOrWhiteSpace(request.Name) ? "새로운 방" : request.Name.Trim(),
                    request.Password,
                    request.HiddenFromList);

                IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
                SetSession(session);
                SetStatus("방을 만들었습니다");
            });
        }

        public async Task JoinRoomAsync(string roomId, string password = null)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                throw new ArgumentException("참가할 방을 선택해주세요");

            await RunAsync(async () =>
            {
                await EnsureInitializedAsync();
                _network.Prepare();
                SetStatus("방에 참가하는 중...");

                JoinSessionOptions options = new JoinSessionOptions
                {
                    Type = SessionType,
                    Password = EmptyToNull(password)
                };
                ISession session = await MultiplayerService.Instance.JoinSessionByIdAsync(roomId, options);
                SetSession(session);
                SetStatus("방에 참가했습니다");
            });
        }

        public async Task JoinByCodeAsync(string joinCode, string password = null)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
                throw new ArgumentException("참가 코드를 입력해주세요");

            await RunAsync(async () =>
            {
                await EnsureInitializedAsync();
                _network.Prepare();
                SetStatus("참가 코드로 연결하는 중...");

                JoinSessionOptions options = new JoinSessionOptions
                {
                    Type = SessionType,
                    Password = EmptyToNull(password)
                };
                ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                    joinCode.Trim().ToUpperInvariant(),
                    options);
                SetSession(session);
                SetStatus("방에 참가했습니다");
            });
        }

        public async Task QuickPlayAsync()
        {
            await RunAsync(async () =>
            {
                await EnsureInitializedAsync();
                _network.Prepare();
                SetStatus("같이 플레이할 사람을 찾는 중...");

                QuickJoinOptions quickJoin = new QuickJoinOptions
                {
                    Timeout = TimeSpan.FromSeconds(5),
                    CreateSession = true,
                    Filters = new List<FilterOption>
                    {
                        GameFilter(),
                        new FilterOption(FilterField.HasPassword, "false", FilterOperation.Equal),
                        new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                        new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)
                    }
                };

                string roomName = $"빠른 대전 {UnityEngine.Random.Range(100, 1000)}";
                ISession session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                    quickJoin,
                    CreateOptions(roomName, null, false));
                SetSession(session);
                SetStatus(session.IsHost ? "새 방을 만들고 상대를 기다립니다" : "빠른 대전 방에 참가했습니다");
            });
        }

        public async Task LeaveRoomAsync()
        {
            if (_session == null) return;

            await RunAsync(async () =>
            {
                SetStatus("방에서 나가는 중...");
                ISession leaving = _session;
                await leaving.LeaveAsync();
                SetSession(null);
                SetStatus(string.Empty);
            });
        }

        public async Task StartGameAsync(string sceneName)
        {
            if (_session == null || !_session.IsHost)
                throw new InvalidOperationException("방장만 게임을 시작할 수 있습니다");
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("시작할 맵이 없습니다");

            await RunAsync(async () =>
            {
                IHostSession host = _session.AsHost();
                host.IsLocked = true;
                await host.SavePropertiesAsync();
                SetStatus("게임을 시작합니다");
                _network.LoadScene(sceneName);
            });
        }

        async Task EnsureInitializedAsync()
        {
            if (_initializeTask == null)
                _initializeTask = InitializeServicesAsync();

            try
            {
                await _initializeTask;
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
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        async Task RunAsync(Func<Task> operation)
        {
            if (_busy) throw new InvalidOperationException("이전 작업이 끝날 때까지 잠시 기다려주세요");

            _busy = true;
            Changed?.Invoke();
            try
            {
                await operation();
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

        static SessionOptions CreateOptions(string name, string password, bool isPrivate)
        {
            SessionOptions options = new SessionOptions
            {
                Type = SessionType,
                Name = name,
                MaxPlayers = PlayerLimit,
                Password = EmptyToNull(password),
                IsPrivate = isPrivate,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    {
                        GameProperty,
                        new SessionProperty(
                            GamePropertyValue,
                            VisibilityPropertyOptions.Public,
                            PropertyIndex.String1)
                    }
                }
            };
            return options.WithRelayNetwork();
        }

        static FilterOption GameFilter()
        {
            return new FilterOption(FilterField.StringIndex1, GamePropertyValue, FilterOperation.Equal);
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
            _status = value ?? string.Empty;
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
            if (length < 8 || length > 64)
                throw new ArgumentException("비밀번호는 8자 이상 64자 이하로 입력해주세요");
        }

        public static string ToReadableMessage(Exception exception)
        {
            if (exception is ArgumentException || exception is InvalidOperationException)
                return exception.Message;

            if (exception is SessionException sessionException)
            {
                switch (sessionException.Error)
                {
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
