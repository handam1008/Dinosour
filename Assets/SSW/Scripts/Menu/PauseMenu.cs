using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SSW
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject _panel;
        [SerializeField] Button _resume;
        [SerializeField] Button _exit;

        PlayerInput _input;
        MapSelector _selector;
        float _timeScale;
        bool _inputActive;
        bool _paused;

        public bool IsOpen => _paused;

        void Awake()
        {
            _resume.onClick.AddListener(Resume);
            _exit.onClick.AddListener(Exit);
            _panel.SetActive(false);
        }

        public void Initialize(PlayerInput input, MapSelector selector)
        {
            _input = input;
            _selector = selector;
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (_paused) Resume();
            else Open();
        }

        public void Open()
        {
            if (_paused || _selector.IsOpen || Time.timeScale <= 0f || !MapTravel.IsLocal) return;
            _timeScale = Time.timeScale;
            _inputActive = _input.inputIsActive;
            _paused = true;
            Time.timeScale = 0f;
            _input.DeactivateInput();
            _panel.SetActive(true);
        }

        public void Resume()
        {
            if (!_paused) return;
            _panel.SetActive(false);
            Restore();
        }

        public void Exit()
        {
            Resume();
            MapTravel.LoadScene("MainMenu");
        }

        void OnDisable()
        {
            Restore();
        }

        void Restore()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = _timeScale;
            if (_inputActive && _input != null) _input.ActivateInput();
        }
    }
}
