using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class SandboxMapMenuUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] Button _practiceButton;
        [SerializeField] Button _backButton;

        string _sceneName;

        public CanvasGroup Group => _group != null ? _group : GetComponent<CanvasGroup>();

        public void Initialize(MainMenuController menu, string sceneName)
        {
            _sceneName = sceneName;
            _practiceButton.onClick.RemoveAllListeners();
            _practiceButton.onClick.AddListener(OpenPracticeMap);
            _practiceButton.interactable = !string.IsNullOrWhiteSpace(_sceneName);

            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(menu.ShowPlay);
        }

        void OpenPracticeMap()
        {
            if (!string.IsNullOrWhiteSpace(_sceneName))
                SceneManager.LoadScene(_sceneName);
        }
    }
}
