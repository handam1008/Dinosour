using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class MapSelectUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] RectTransform _container;
        [SerializeField] GameObject _buttonTemplate;
        [SerializeField] float _fadeDuration = 0.15f;

        float _previousTimeScale = 1f;
        bool _paused;

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
            _previousTimeScale = Time.timeScale;
            _paused = true;
            Time.timeScale = 0f;

            _group.alpha = 0f;
            _group.DOFade(1f, _fadeDuration).SetUpdate(true);

            if (maps == null) return;
            foreach (MapEntry map in maps)
            {
                if (map == null || string.IsNullOrEmpty(map.sceneName)) continue;

                GameObject buttonGo = Instantiate(_buttonTemplate, _container);
                buttonGo.SetActive(true);
                buttonGo.GetComponentInChildren<Text>().text = map.displayName;

                string sceneName = map.sceneName;
                buttonGo.GetComponent<Button>().onClick.AddListener(() => LoadMap(sceneName));
            }
        }

        public void Close()
        {
            RestoreTimeScale();
            Destroy(gameObject);
        }

        void LoadMap(string sceneName)
        {
            RestoreTimeScale();
            SceneManager.LoadScene(sceneName);
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
