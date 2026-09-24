using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace SSW
{
    public class MapSelectUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] RectTransform _container;
        [SerializeField] GameObject _buttonTemplate;
        [SerializeField] float _fadeDuration = 0.15f;
        [SerializeField] MapCatalog _catalog;

        float _previousTimeScale = 1f;
        bool _paused;
        int _openedFrame;

        void Awake()
        {
            if (EventSystem.current == null)
            {
                GameObject go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
        }

        public void Show(MapEntry[] maps)
        {
            _openedFrame = Time.frameCount;
            _previousTimeScale = Time.timeScale;
            _paused = true;
            Time.timeScale = 0f;
            _group.alpha = 0f;
            _group.DOFade(1f, _fadeDuration).SetUpdate(true);

            foreach (MapLayout map in _catalog.Maps)
            {
                Button button = AddButton(map.Title);
                button.interactable = MapTravel.IsLocal;
                button.onClick.AddListener(() => OpenMap(map));
            }

            if (maps == null) return;
            foreach (MapEntry map in maps)
            {
                if (map == null || string.IsNullOrEmpty(map.sceneName)) continue;
                string sceneName = map.sceneName;
                Button button = AddButton(map.displayName);
                button.onClick.AddListener(() => LoadMap(sceneName));
            }
        }

        Button AddButton(string title)
        {
            GameObject buttonGo = Instantiate(_buttonTemplate, _container);
            buttonGo.SetActive(true);
            buttonGo.GetComponentInChildren<Text>().text = title;
            return buttonGo.GetComponent<Button>();
        }

        void Update()
        {
            if (Time.frameCount == _openedFrame || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame)
                Close();
        }
        public void Close()
        {
            RestoreTimeScale();
            Destroy(gameObject);
        }

        void LoadMap(string sceneName)
        {
            RestoreTimeScale();
            MapTravel.LoadScene(sceneName);
        }

        void OpenMap(MapLayout map)
        {
            RestoreTimeScale();
            MapTravel.Open(map);
        }

        void OnDisable()
        {
            RestoreTimeScale();
        }

        void RestoreTimeScale()
        {
            if (!_paused) return;
            Time.timeScale = _previousTimeScale;
            _paused = false;
        }
    }
}
