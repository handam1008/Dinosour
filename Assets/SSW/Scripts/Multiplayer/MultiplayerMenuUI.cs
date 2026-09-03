using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerMenuUI : MonoBehaviour
    {
        enum Page
        {
            Create,
            Browse,
            Password,
            Matching,
            Waiting
        }

        [SerializeField] CanvasGroup _group;
        [SerializeField] Text _statusText;
        [SerializeField] Button _backButton;

        [SerializeField] GameObject _createPage;
        [SerializeField] InputField _roomNameInput;
        [SerializeField] InputField _createPasswordInput;
        [SerializeField] Toggle _hiddenToggle;
        [SerializeField] Button _createButton;

        [SerializeField] GameObject _browsePage;
        [SerializeField] Button _refreshButton;
        [SerializeField] InputField _joinCodeInput;
        [SerializeField] InputField _joinCodePasswordInput;
        [SerializeField] Button _joinCodeButton;
        [SerializeField] RectTransform _roomContent;
        [SerializeField] Text _emptyRoomText;
        [SerializeField] MultiplayerRoomRowUI _roomRowPrefab;

        [SerializeField] GameObject _passwordPage;
        [SerializeField] Text _passwordRoomNameText;
        [SerializeField] Text _passwordPlayerCountText;
        [SerializeField] InputField _roomPasswordInput;
        [SerializeField] Button _passwordJoinButton;
        [SerializeField] Button _passwordBackButton;

        [SerializeField] GameObject _matchingPage;

        [SerializeField] GameObject _waitingPage;
        [SerializeField] Text _waitingRoomNameText;
        [SerializeField] Text _joinCodeText;
        [SerializeField] Text _waitingPlayerCountText;
        [SerializeField] Text _playerListText;
        [SerializeField] Text _waitingMessageText;
        [SerializeField] Button _startButton;
        [SerializeField] Button _leaveButton;

        readonly List<MultiplayerRoomRowUI> _roomRows = new List<MultiplayerRoomRowUI>();

        MainMenuController _menu;
        MultiplayerSessionManager _sessions;
        string _gameSceneName;
        string _passwordRoomId;
        Page _page;

        public CanvasGroup Group => _group != null ? _group : GetComponent<CanvasGroup>();

        void Awake()
        {
            _backButton.onClick.AddListener(Back);
            _createButton.onClick.AddListener(CreateRoom);
            _refreshButton.onClick.AddListener(RefreshRooms);
            _joinCodeButton.onClick.AddListener(JoinByCode);
            _passwordJoinButton.onClick.AddListener(JoinPasswordRoom);
            _passwordBackButton.onClick.AddListener(ShowBrowse);
            _startButton.onClick.AddListener(StartGame);
            _leaveButton.onClick.AddListener(LeaveRoom);
        }

        public void Initialize(MainMenuController menu, string gameSceneName)
        {
            _menu = menu;
            _gameSceneName = gameSceneName;

            if (_sessions != null) _sessions.Changed -= HandleSessionChanged;
            _sessions = MultiplayerSessionManager.GetOrCreate();
            _sessions.Changed += HandleSessionChanged;
            UpdateStatus();
        }

        public void ShowCreate()
        {
            if (string.IsNullOrWhiteSpace(_roomNameInput.text))
                _roomNameInput.text = $"공룡 방 {UnityEngine.Random.Range(100, 1000)}";

            ShowPage(Page.Create);
        }

        public void ShowBrowse()
        {
            ShowPage(Page.Browse);
            RefreshRooms();
        }

        public void ShowQuickPlay()
        {
            ShowPage(Page.Matching);
            QuickPlay();
        }

        void ShowPage(Page page)
        {
            _page = page;
            _createPage.SetActive(false);
            _browsePage.SetActive(false);
            _passwordPage.SetActive(false);
            _matchingPage.SetActive(false);
            _waitingPage.SetActive(false);

            GameObject target = page switch
            {
                Page.Create => _createPage,
                Page.Browse => _browsePage,
                Page.Password => _passwordPage,
                Page.Matching => _matchingPage,
                _ => _waitingPage
            };

            target.SetActive(true);
            AnimatePage(target);
            UpdateStatus();
        }

        void AnimatePage(GameObject page)
        {
            RectTransform rect = page.transform as RectTransform;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            rect.DOKill();
            rect.localScale = Vector3.one * 0.96f;
            rect.DOScale(1f, 0.28f).SetEase(Ease.OutBack);

            if (pageGroup != null)
            {
                pageGroup.DOKill();
                pageGroup.alpha = 0f;
                pageGroup.DOFade(1f, 0.2f);
            }

            Button[] buttons = page.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform button = buttons[i].transform;
                button.DOKill();
                button.localScale = Vector3.zero;
                button.DOScale(1f, 0.28f)
                    .SetDelay(i * 0.06f)
                    .SetEase(Ease.OutBack);
            }
        }

        void ShowPasswordRoom(MultiplayerRoomInfo room)
        {
            _passwordRoomId = room.Id;
            _passwordRoomNameText.text = room.Name;
            _passwordPlayerCountText.text = $"{room.PlayerCount} / 2명";
            _roomPasswordInput.text = string.Empty;
            ShowPage(Page.Password);
        }

        void ShowWaitingRoom()
        {
            ShowPage(Page.Waiting);
            UpdateWaitingRoom();
        }

        void RebuildRoomList()
        {
            foreach (MultiplayerRoomRowUI row in _roomRows)
            {
                if (row == null) continue;
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }

            _roomRows.Clear();
            _emptyRoomText.gameObject.SetActive(_sessions.Rooms.Count == 0);

            foreach (MultiplayerRoomInfo room in _sessions.Rooms)
            {
                MultiplayerRoomRowUI row = Instantiate(_roomRowPrefab, _roomContent);
                row.Bind(room, JoinSelectedRoom);
                _roomRows.Add(row);
            }
        }

        void JoinSelectedRoom(MultiplayerRoomInfo room)
        {
            if (room.HasPassword)
            {
                ShowPasswordRoom(room);
                return;
            }

            JoinRoom(room.Id, null);
        }

        void UpdateWaitingRoom()
        {
            _waitingRoomNameText.text = _sessions.RoomName;
            _joinCodeText.text = $"참가 코드  {_sessions.JoinCode}";
            _waitingPlayerCountText.text = $"{_sessions.PlayerCount} / 2명";

            StringBuilder players = new StringBuilder();
            for (int i = 0; i < _sessions.PlayerCount; i++)
                players.AppendLine(i == 0 ? "● 플레이어 1  (방장)" : $"● 플레이어 {i + 1}");
            _playerListText.text = players.ToString();

            _startButton.gameObject.SetActive(_sessions.IsHost);
            _startButton.interactable = _sessions.IsHost
                && _sessions.PlayerCount == 2
                && _sessions.HasNetworkPlayer
                && !_sessions.IsBusy;

            if (!_sessions.IsHost)
                _waitingMessageText.text = "방장이 게임을 시작할 때까지 기다려주세요";
            else if (!_sessions.HasNetworkPlayer)
                _waitingMessageText.text = "잠시만 기다려주세요";
            else if (_sessions.PlayerCount < 2)
                _waitingMessageText.text = "상대를 기다리는 중...";
            else
                _waitingMessageText.text = "상대가 들어왔어요. 게임을 시작해 주세요!";
        }

        async void CreateRoom()
        {
            try
            {
                await _sessions.CreateRoomAsync(new MultiplayerRoomRequest(
                    _roomNameInput.text,
                    _createPasswordInput.text,
                    _hiddenToggle.isOn));
            }
            catch (Exception)
            {
            }
        }

        async void RefreshRooms()
        {
            try
            {
                await _sessions.RefreshRoomsAsync();
                if (_page == Page.Browse) RebuildRoomList();
            }
            catch (Exception)
            {
            }
        }

        async void JoinRoom(string roomId, string password)
        {
            try
            {
                await _sessions.JoinRoomAsync(roomId, password);
            }
            catch (Exception)
            {
            }
        }

        void JoinPasswordRoom()
        {
            JoinRoom(_passwordRoomId, _roomPasswordInput.text);
        }

        async void JoinByCode()
        {
            try
            {
                await _sessions.JoinByCodeAsync(_joinCodeInput.text, _joinCodePasswordInput.text);
            }
            catch (Exception)
            {
            }
        }

        async void QuickPlay()
        {
            try
            {
                await _sessions.QuickPlayAsync();
            }
            catch (Exception)
            {
            }
        }

        async void LeaveRoom()
        {
            try
            {
                await _sessions.LeaveRoomAsync();
                _menu.ShowPlay();
            }
            catch (Exception)
            {
            }
        }

        async void StartGame()
        {
            try
            {
                await _sessions.StartGameAsync(_gameSceneName);
            }
            catch (Exception)
            {
            }
        }

        void Back()
        {
            if (_sessions.IsBusy) return;
            if (_sessions.IsInSession)
            {
                LeaveRoom();
                return;
            }

            _menu.ShowPlay();
        }

        void HandleSessionChanged()
        {
            UpdateStatus();
            if (!_sessions.IsInSession) return;

            if (_page != Page.Waiting) ShowWaitingRoom();
            else UpdateWaitingRoom();
        }

        void UpdateStatus()
        {
            if (_sessions == null) return;
            _statusText.text = _sessions.Status;
            _createButton.interactable = !_sessions.IsBusy;
            _refreshButton.interactable = !_sessions.IsBusy;
            _joinCodeButton.interactable = !_sessions.IsBusy;
            _passwordJoinButton.interactable = !_sessions.IsBusy;
            _leaveButton.interactable = !_sessions.IsBusy;
        }

        void OnDestroy()
        {
            transform.DOKill(true);
            if (_sessions != null) _sessions.Changed -= HandleSessionChanged;
        }
    }
}
