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
        [SerializeField] RectTransform _title;
        [SerializeField] Text _statusText;
        [SerializeField] MultiplayerMenuUI _multiplayerMenuPrefab;
        [SerializeField] SandboxMapMenuUI _sandboxMapMenuPrefab;
        [SerializeField] string _sandboxSceneName = "SuperUltraLegendScene";
        [SerializeField] float _fadeDuration = 0.22f;
        [SerializeField] float _buttonStagger = 0.07f;

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
            _mainPanel.gameObject.SetActive(false);
            _playPanel.gameObject.SetActive(false);
            _jobPanel.gameObject.SetActive(false);
            _settingsPanel.gameObject.SetActive(false);
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
            ShowPanel(_settingsPanel);
        }

        void ShowPanel(CanvasGroup target)
        {
            if (_current == target) return;
            _title.gameObject.SetActive(target != _jobPanel);
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

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        public void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
        }

        public void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;
        }

        void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
        }
    }
}
