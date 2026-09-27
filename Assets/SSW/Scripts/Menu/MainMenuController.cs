using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] CanvasGroup _mainPanel;
        [SerializeField] CanvasGroup _playPanel;
        [SerializeField] CanvasGroup _jobPanel;
        [SerializeField] JobSettingsPanel _jobSettingsPanel;
        [SerializeField] CanvasGroup _settingsPanel;
        [SerializeField] CanvasGroup _leaderboardPanel;
        [SerializeField] LeaderboardPanel _leaderboard;
        [SerializeField] CanvasGroup _customPanel;
        [SerializeField] RectTransform _title;
        [SerializeField] Text _statusText;
        [SerializeField] MultiplayerMenuUI _multiplayerMenuPrefab;
        [SerializeField] SandboxMapMenuUI _sandboxMapMenuPrefab;
        [SerializeField] string _sandboxSceneName = "SuperUltraLegendScene";
        [SerializeField] Slider _masterVolume;
        [SerializeField] Slider _bgmVolume;
        [SerializeField] Slider _sfxVolume;
        [SerializeField] float _fadeDuration = 0.22f;
        [SerializeField] float _buttonStagger = 0.07f;
        [SerializeField] CanvasGroup _titleboardPanel;
        
        [SerializeField] SoundCue _btnClickSoundCue;
        [SerializeField] private SoundCue _matchFoundSoundCue;

        MultiplayerMenuUI _multiplayerMenu;
        SandboxMapMenuUI _sandboxMapMenu;

        CanvasGroup _current;

        void OnEnable()
        {
            if (_jobSettingsPanel != null) _jobSettingsPanel.BackRequested += ShowMain;
        }

        void OnDisable()
        {
            if (_jobSettingsPanel != null) _jobSettingsPanel.BackRequested -= ShowMain;
        }

        void Start()
        {
            GameAudio audio = GameAudio.GetOrCreate();
            audio.PlayBgm(audio.Bank.Menu);
            SyncVolume();
            _mainPanel.gameObject.SetActive(false);
            _playPanel.gameObject.SetActive(false);
            _jobPanel.gameObject.SetActive(false);
            _settingsPanel.gameObject.SetActive(false);
            if (_leaderboardPanel != null) _leaderboardPanel.gameObject.SetActive(false);
            if (_customPanel != null) _customPanel.gameObject.SetActive(false);
            ShowMain();
            PlayTitleIntro();
        }

        void PlayTitleIntro()
        {
            if (_title == null) return;
            Vector2 pos = _title.anchoredPosition;
            _title.anchoredPosition = pos + new Vector2(0f, 260f);
            _title.DOAnchorPos(pos, 0.65f).SetEase(Ease.OutBack).SetLink(_title.gameObject, LinkBehaviour.KillOnDestroy).OnComplete(() =>
                _title.DOAnchorPosY(pos.y + 12f, 1.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(_title.gameObject, LinkBehaviour.KillOnDestroy));
        }

        public void ShowMain()
        {
            ShowPanel(_mainPanel);
        }

        public void ShowPlay()
        {
            ShowPanel(_playPanel);
        }

        public void ShowJobs()
        {
            ShowPanel(_jobPanel);
            _jobSettingsPanel.Open();
        }

        public void ShowSettings()
        {
            SyncVolume();
            ShowPanel(_settingsPanel);
        }

        public void ShowLeaderboard()
        {
            ShowPanel(_leaderboardPanel);
            _leaderboard.Refresh();
        }

        public void ShowCustom()
        {
            if (_customPanel == null) return;
            ShowPanel(_customPanel);
        }

        void ShowPanel(CanvasGroup target)
        {
            GameAudio.Current.PlaySfx(_btnClickSoundCue);
            if (_current == target) return;
            _title.gameObject.SetActive(target != _jobPanel && target != _leaderboardPanel && target != _customPanel);
            CanvasGroup previous = _current;
            if (previous != null)
            {
                previous.DOKill();
                previous.interactable = false;
                previous.blocksRaycasts = false;
                previous.DOFade(0f, _fadeDuration * 0.5f)
                    .SetUpdate(true).SetLink(previous.gameObject)
                    .OnComplete(() => previous.gameObject.SetActive(false));
            }

            _current = target;
            target.DOKill();
            target.gameObject.SetActive(true);
            target.alpha = 0f;
            target.interactable = false;
            target.blocksRaycasts = false;
            target.DOFade(1f, _fadeDuration)
                .SetDelay(previous != null ? _fadeDuration * 0.5f : 0f)
                .SetUpdate(true).SetLink(target.gameObject)
                .OnComplete(() =>
                {
                    target.interactable = true;
                    target.blocksRaycasts = true;
                });
            if (_multiplayerMenu == null || target != _multiplayerMenu.Group)
                AnimateButtons(target);
            SetStatus("");
        }

        void AnimateButtons(CanvasGroup panel)
        {
            Button[] buttons = panel.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform t = buttons[i].transform;
                t.DOKill();
                t.localScale = Vector3.zero;
                t.DOScale(1f, 0.32f).SetDelay(i * _buttonStagger).SetEase(Ease.OutBack).SetLink(t.gameObject);
            }
        }

        public void PlaySandbox()
        {
            ShowSandboxMaps();
        }

        void ShowSandboxMaps()
        {
            SandboxMapMenuUI menu = GetSandboxMapMenu();
            if (menu != null) ShowPanel(menu.Group);
        }

        public void CreateRoom()
        {
            MultiplayerMenuUI menu = GetMultiplayerMenu();
            if (menu == null) return;
            menu.ShowCreate();
            ShowPanel(menu.Group);
        }

        public void JoinRoom()
        {
            MultiplayerMenuUI menu = GetMultiplayerMenu();
            if (menu == null) return;
            menu.ShowBrowse();
            ShowPanel(menu.Group);
        }

        public void StartMatchmaking()
        {
            MultiplayerMenuUI menu = GetMultiplayerMenu();
            if (menu == null) return;
            menu.ShowQuickPlay();
            ShowPanel(menu.Group);
        }
        public void ShowTitleboard()
        {
            ShowPanel(_titleboardPanel);
        }

        MultiplayerMenuUI GetMultiplayerMenu()
        {
            if (_multiplayerMenu == null)
            {
                if (_multiplayerMenuPrefab == null)
                    _multiplayerMenuPrefab = Resources.Load<MultiplayerMenuUI>("UI/MultiplayerMenu");
                if (_multiplayerMenuPrefab == null)
                {
                    SetStatus("멀티플레이 화면을 불러오지 못했습니다");
                    return null;
                }

                RectTransform parent = _mainPanel.transform.parent as RectTransform;
                _multiplayerMenu = Instantiate(_multiplayerMenuPrefab, parent, false);
                _multiplayerMenu.Initialize(this, _sandboxSceneName);
                _multiplayerMenu.gameObject.SetActive(false);
            }

            return _multiplayerMenu;
        }

        SandboxMapMenuUI GetSandboxMapMenu()
        {
            if (_sandboxMapMenu == null)
            {
                if (_sandboxMapMenuPrefab == null)
                    _sandboxMapMenuPrefab = Resources.Load<SandboxMapMenuUI>("UI/SandboxMapMenu");
                if (_sandboxMapMenuPrefab == null)
                {
                    SetStatus("샌드박스 화면을 불러오지 못했습니다");
                    return null;
                }

                RectTransform parent = _mainPanel.transform.parent as RectTransform;
                _sandboxMapMenu = Instantiate(_sandboxMapMenuPrefab, parent, false);
                _sandboxMapMenu.Initialize(this, _sandboxSceneName);
                _sandboxMapMenu.gameObject.SetActive(false);
            }

            return _sandboxMapMenu;
        }
        
        public void PlayMatchFoundSound()
        {
            GameAudio.Current.PlaySfx(_matchFoundSoundCue);
        }

        public void SetMasterVolume(float value)
        {
            GameAudio.GetOrCreate().SetMasterVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            GameAudio.GetOrCreate().SetSfxVolume(value);
        }

        public void SetBgmVolume(float value)
        {
            GameAudio.GetOrCreate().SetBgmVolume(value);
        }

        void SyncVolume()
        {
            GameAudio audio = GameAudio.GetOrCreate();
            _masterVolume.SetValueWithoutNotify(audio.MasterVolume);
            _bgmVolume.SetValueWithoutNotify(audio.BgmVolume);
            _sfxVolume.SetValueWithoutNotify(audio.SfxVolume);
        }

        public void SetFullscreen(bool value)
        {
            GameAudio.Current.PlaySfx(_btnClickSoundCue);
            Screen.fullScreen = value;
        }

        void SetStatus(string message)
        {
            GameAudio.Current.PlaySfx(_btnClickSoundCue);
            if (_statusText != null) _statusText.text = message;
        }
    }
}
