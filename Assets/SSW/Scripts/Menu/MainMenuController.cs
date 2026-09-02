using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] string _sandboxSceneName = "SuperUltraLegendScene";
        [SerializeField] float _fadeDuration = 0.22f;
        [SerializeField] float _buttonStagger = 0.07f;

        CanvasGroup _mapPanel;
        MultiplayerMenuUI _multiplayerMenu;

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
            _title.DOAnchorPos(pos, 0.65f).SetEase(Ease.OutBack).OnComplete(() =>
                _title.DOAnchorPosY(pos.y + 12f, 1.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
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

            if (_title != null) _title.gameObject.SetActive(target != _jobPanel);

            if (_current != null)
            {
                _current.DOKill();
                _current.gameObject.SetActive(false);
            }

            _current = target;
            target.gameObject.SetActive(true);
            target.DOKill();
            target.alpha = 0f;
            target.DOFade(1f, _fadeDuration);
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
                t.DOScale(1f, 0.32f).SetDelay(i * _buttonStagger).SetEase(Ease.OutBack);
            }
        }

        public void PlaySandbox()
        {
            ShowSandboxMaps();
        }

        void ShowSandboxMaps()
        {
            if (_mapPanel == null) _mapPanel = CreateMapPanel();
            ShowPanel(_mapPanel);
        }

        CanvasGroup CreateMapPanel()
        {
            GameObject panelObject = new GameObject("SandboxMapPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelObject.transform.SetParent(_mainPanel.transform.parent, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.09f, 0.97f);

            CreateLabel(panel, "SANDBOX MAPS", new Vector2(0f, 250f), 48, new Color(0.45f, 0.95f, 1f));
            CreateLabel(panel, "맵을 선택하세요", new Vector2(0f, 202f), 22, new Color(0.75f, 0.8f, 0.95f));

            CreateMapCard(panel, "NEON TEST ARENA", "넓은 이동 · 더미 테스트 · 점프 구역", new Vector2(0f, 40f), _sandboxSceneName);
            CreateMapCard(panel, "COMING SOON", "새로운 샌드박스 구역 준비 중", new Vector2(0f, -105f), string.Empty);
            CreateBackButton(panel);
            return panelObject.GetComponent<CanvasGroup>();
        }

        void CreateMapCard(RectTransform parent, string title, string subtitle, Vector2 position, string sceneName)
        {
            GameObject cardObject = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            cardObject.transform.SetParent(parent, false);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.sizeDelta = new Vector2(620f, 118f);
            card.anchoredPosition = position;
            cardObject.GetComponent<Image>().color = string.IsNullOrEmpty(sceneName)
                ? new Color(0.09f, 0.1f, 0.18f, 0.72f)
                : new Color(0.08f, 0.18f, 0.3f, 0.96f);
            Outline outline = cardObject.GetComponent<Outline>();
            outline.effectColor = string.IsNullOrEmpty(sceneName) ? new Color(0.3f, 0.35f, 0.5f) : new Color(0.2f, 0.9f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            CreateLabel(card, title, new Vector2(0f, 20f), 28, Color.white);
            CreateLabel(card, subtitle, new Vector2(0f, -23f), 17, new Color(0.62f, 0.8f, 0.95f));

            Button button = cardObject.GetComponent<Button>();
            button.interactable = !string.IsNullOrEmpty(sceneName);
            if (button.interactable) button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
        }

        void CreateBackButton(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 54f);
            rect.anchoredPosition = new Vector2(0f, -245f);
            buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.28f, 0.95f);
            buttonObject.GetComponent<Button>().onClick.AddListener(ShowPlay);
            CreateLabel(rect, "뒤로", Vector2.zero, 21, Color.white);
        }

        static void CreateLabel(RectTransform parent, string text, Vector2 position, int size, Color color)
        {
            GameObject labelObject = new GameObject(text, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(580f, 48f);
            rect.anchoredPosition = position;
            Text label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
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
