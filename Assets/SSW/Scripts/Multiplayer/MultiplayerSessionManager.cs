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
        static string GamePropertyValue => $"mushrooms-1v1-v{NetGame.Protocol}";
        const string ModeProperty = "mode";
        const int PlayerLimit = 2;
        static MultiplayerSessionManager _instance;
        string _sessionType = SessionType;
        readonly RoomCatalog _rooms = new RoomCatalog();
        NetGame _network;
        Task _initializeTask;
        Task _operation = Task.CompletedTask;
        Task _roomQuery = Task.CompletedTask;
        ISession _session;
        bool _busy;
        bool _destroyed;
        bool _cancelled;
        bool _matching;
        bool _starting;
        bool _closing;
        bool _ended;
        bool _quitting;
        bool _quitReady;
        double _closeAt;
        bool _listing;
        int _revision;
        string _status = string.Empty;

        public static MultiplayerSessionManager Current => _instance;
        public event Action Changed;
        public IReadOnlyList<MultiplayerRoomInfo> Rooms => _rooms.Items;
        public int RoomsVersion => _rooms.Version;
        public bool IsBusy => _busy;
        public bool IsRefreshing => _listing;
        public bool IsMatching => _matching;
        public bool IsInSession => _session != null;
        public bool IsHost => _session != null && _session.IsHost;
        public bool CanStart => IsHost && _network.Ready && !_busy && !_starting;
        public string Status => _status;
        public string RoomName => _session != null ? _session.Name : string.Empty;
        public string RoomId => _session != null ? _session.Id : string.Empty;
        public string JoinCode => _session != null ? _session.Code : string.Empty;
        public int PlayerCount => _session != null ? _session.PlayerCount : 0;
        public bool HasNetworkPlayer => _network != null && _network.HasPlayerPrefab;
        public string OpponentPlayerId => _session?.Players.FirstOrDefault(player => player.Id != AuthenticationService.Instance.PlayerId)?.Id;

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
            Application.wantsToQuit += WantsToQuit;
        }

        public Task RefreshRoomsAsync(bool silent = false)
        {
            if (_destroyed) return Task.FromException(new ObjectDisposedException(nameof(MultiplayerSessionManager)));
            if (_busy || IsInSession) return Task.CompletedTask;
            if (_listing) return _roomQuery;
            _listing = true;
            NotifyChanged();
            _roomQuery = ReadRooms(silent, _revision);
            return _roomQuery;
        }

        async Task ReadRooms(bool silent, int revision)
        {
            if (!silent) SetStatus("방 목록을 불러오는 중...");
            try
            {
                await EnsureInitializedAsync();
                if (!CanApplyRooms(revision)) return;
                QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(Query("room"));
                if (!CanApplyRooms(revision)) return;
                _rooms.Replace(result.Sessions.Select(room => new MultiplayerRoomInfo(
                    room.Id, room.Name, room.MaxPlayers - room.AvailableSlots, room.HasPassword)), Time.unscaledTimeAsDouble);
                SetStatus(Rooms.Count == 0 ? "현재 참가할 수 있는 방이 없습니다" : string.Empty);
            }
            catch (Exception error)
            {
                if (CanApplyRooms(revision))
                {
                    _rooms.Clear();
                    SetStatus(ToReadableMessage(error));
                }
                throw;
            }
            finally
            {
                _listing = false;
                NotifyChanged();
            }
        }

        bool CanApplyRooms(int revision)
        {
            return !_destroyed && !_busy && !IsInSession && revision == _revision;
        }

        public Task CreateRoomAsync(MultiplayerRoomRequest request)
        {
            return RunAsync(async () =>
            {
                ValidatePassword(request.Password);
                SetStatus("방을 만드는 중...");
                await EnsureInitializedAsync();
                CheckCancel();
                await PrepareNetwork();
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
                await PrepareNetwork();
                try
                {
                    var session = await MultiplayerService.Instance.JoinSessionByIdAsync(roomId, JoinOptions(password));
                    await Accept(session);
                }
                catch (SessionException error) when (Unavailable(error))
                {
                    _rooms.Reject(roomId, Time.unscaledTimeAsDouble);
                    NotifyChanged();
                    await Drop();
                    throw;
                }
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
                await PrepareNetwork();
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
                    await PrepareNetwork();
                    double nextSearch = 0;
                    int failures = 0;
                    while (_network.Arena == null)
                    {
                        CheckCancel();
                        try
                        {
                            if (_session != null && !_network.Connected)
                                throw new SessionException("대기 중 연결이 끊어졌습니다.", SessionError.NetworkManagerStartFailed, null);
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
                        }
                        catch (SessionException error) when (failures < 2 && CanRetry(error))
                        {
                            failures++;
                            await Drop();
                            CheckCancel();
                            SetStatus("연결을 다시 시도하는 중...");
                            await Task.Delay(500 * failures);
                            nextSearch = 0;
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
            await PrepareNetwork();
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

        static bool CanRetry(SessionException error)
        {
            return error.Error == SessionError.NetworkManagerStartFailed ||
                error.Error == SessionError.NetworkSetupFailed;
        }

        static bool Unavailable(SessionException error)
        {
            return error.Error == SessionError.SessionNotFound || error.Error == SessionError.SessionDeleted ||
                error.Error == SessionError.SessionConflict || CanRetry(error);
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
            await PrepareNetwork();
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
            await LeaveRoomAsync();
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
            SetSession(session);
            if (_cancelled || _destroyed)
            {
                await Drop();
                CheckCancel();
            }
            if (!_network.Connected)
            {
                await Drop();
                throw new SessionException("방 연결이 종료되었습니다", SessionError.NetworkManagerStartFailed, null);
            }
            if (_matching) SetStatus("상대를 찾는 중...");
        }

        async Task PrepareNetwork()
        {
            await Drop();
            CheckCancel();
            await _network.PrepareSessionAsync();
            CheckCancel();
        }

        async Task Drop()
        {
            ISession leaving = _session;
            if (leaving == null && UnityServices.State == ServicesInitializationState.Initialized)
                MultiplayerService.Instance.Sessions.TryGetValue(_sessionType, out leaving);
            if (leaving == null) return;
            _closing = true;
            try
            {
                await CloseSession(leaving);
                SetSession(null);
            }
            finally
            {
                _closing = false;
            }
        }

        async Task CloseSession(ISession leaving)
        {
            try
            {
                if (leaving.IsHost) await leaving.AsHost().DeleteAsync();
                else await leaving.LeaveAsync();
            }
            catch (SessionException error) when (
                error.Error == SessionError.SessionNotFound ||
                error.Error == SessionError.SessionDeleted ||
                (error.Error == SessionError.InvalidOperation || error.Error == SessionError.NotInLobby) && !leaving.IsMember)
            {
                await Task.Yield();
                if (!MultiplayerService.Instance.Sessions.ContainsKey(leaving.Type)) return;
                if (leaving.IsHost && leaving.IsMember)
                {
                    await leaving.AsHost().DeleteAsync();
                    return;
                }
                var joined = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
                if (joined.Contains(leaving.Id) || leaving.Network.State != NetworkState.Stopped ||
                    _network.Connected || _network.Manager.ShutdownInProgress) throw;
                _sessionType = SessionType + "-" + Guid.NewGuid().ToString("N");
            }
        }

        async Task EnsureInitializedAsync()
        {
            if (_initializeTask == null) _initializeTask = InitializeServicesAsync();
            try
            {
                await _initializeTask;
                CheckCancel();
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
            if (_destroyed) return Task.FromException(new ObjectDisposedException(nameof(MultiplayerSessionManager)));
            if (_busy) return Task.FromException(new InvalidOperationException("이전 작업이 끝날 때까지 잠시 기다려주세요"));
            _busy = true;
            _revision++;
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
            if (_cancelled || _destroyed) throw new OperationCanceledException();
        }

        SessionOptions CreateOptions(string name, string password, bool isPrivate, string mode)
        {
            return new SessionOptions
            {
                Type = _sessionType,
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

        JoinSessionOptions JoinOptions(string password)
        {
            return new JoinSessionOptions { Type = _sessionType, Password = EmptyToNull(password) };
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
            UnbindSession();
            _session = session;
            _starting = false;
            _ended = false;
            _closeAt = 0;
            if (_session != null)
            {
                _session.Changed += NotifyChanged;
                _session.RemovedFromSession += HandleSessionEnded;
                _session.Deleted += HandleSessionEnded;
                _session.SessionHostChanged += HandleHostChanged;
            }
            Changed?.Invoke();
        }

        void UnbindSession()
        {
            if (_session == null) return;
            _session.Changed -= NotifyChanged;
            _session.RemovedFromSession -= HandleSessionEnded;
            _session.Deleted -= HandleSessionEnded;
            _session.SessionHostChanged -= HandleHostChanged;
        }

        void NotifyChanged()
        {
            if (!_closing && _session != null && !_network.Connected) _ended = true;
            Changed?.Invoke();
        }

        void HandleSessionEnded()
        {
            if (!_closing) _ended = true;
        }

        void HandleHostChanged(string host)
        {
            HandleSessionEnded();
        }

        void Update()
        {
            if (!_ended || _busy || _matching || _quitting || Time.unscaledTimeAsDouble < _closeAt) return;
            _ended = false;
            _ = EndSession();
        }

        async Task EndSession()
        {
            try
            {
                await RunAsync(async () =>
                {
                    await Drop();
                    SetStatus("방 연결이 종료되었습니다");
                });
            }
            catch (Exception)
            {
                _ended = true;
                _closeAt = Time.unscaledTimeAsDouble + 5d;
            }
        }

        bool WantsToQuit()
        {
            if (_quitReady || _session == null && !_busy) return true;
            if (!_quitting)
            {
                _quitting = true;
                _ = Quit();
            }
            return false;
        }

        async Task Quit()
        {
            Task leave = CancelAsync();
            try
            {
                if (await Task.WhenAny(leave, Task.Delay(5000)) == leave) await leave;
                else _ = Observe(leave);
            }
            catch (Exception) { }
            _quitReady = true;
            Application.Quit();
        }

        static async Task Observe(Task operation)
        {
            try { await operation; }
            catch (Exception) { }
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
            _destroyed = true;
            _cancelled = true;
            Changed = null;
            UnbindSession();
            Application.wantsToQuit -= WantsToQuit;
            if (_network != null) _network.ConnectionChanged -= NotifyChanged;
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
