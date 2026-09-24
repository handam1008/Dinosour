using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class SandboxMapMenuUI : MonoBehaviour
    {
        [System.Serializable]
        struct Choice
        {
            public Button button;
            public MapLayout map;
        }

        [SerializeField] CanvasGroup _group;
        [SerializeField] Button _practiceButton;
        [SerializeField] Button _backButton;
        [SerializeField] Choice[] _maps;

        string _sceneName;

        public CanvasGroup Group => _group;

        public void Initialize(MainMenuController menu, string sceneName)
        {
            _sceneName = sceneName;
            _practiceButton.onClick.RemoveAllListeners();
            _practiceButton.onClick.AddListener(OpenPractice);
            _practiceButton.interactable = !string.IsNullOrWhiteSpace(_sceneName);
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(menu.ShowPlay);

            foreach (Choice choice in _maps)
            {
                choice.button.onClick.RemoveAllListeners();
                choice.button.onClick.AddListener(() => MapTravel.Open(choice.map));
                choice.button.interactable = MapTravel.IsLocal;
            }
        }

        void OpenPractice()
        {
            MapTravel.LoadScene(_sceneName);
        }
    }
}
