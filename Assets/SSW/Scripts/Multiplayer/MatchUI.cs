using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SSW
{
    public sealed class MatchUI : MonoBehaviour
    {
        [SerializeField] GameObject _panel;
        [SerializeField] Text _title;
        [SerializeField] Button _resume;
        [SerializeField] Button _exit;
        NetGame _game;
        bool _result;

        internal bool IsOpen => _panel.activeSelf;
        internal string Title => _title.gameObject.activeSelf ? _title.text : string.Empty;
        internal bool CanResume => _resume.gameObject.activeSelf;

        public void Bind(NetGame game)
        {
            _game = game;
            _resume.onClick.AddListener(Resume);
            _exit.onClick.AddListener(Exit);
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_result || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (_panel.activeSelf) Resume();
            else
            {
                _title.gameObject.SetActive(false);
                _panel.SetActive(true);
                if (_game.Local != null) _game.Local.Block(true);
            }
        }

        public void ShowResult(MatchState state)
        {
            _result = true;
            _panel.SetActive(true);
            _resume.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            bool won = state.Winner == _game.LocalId;
            _title.text = state.Reason == MatchEnd.Draw ? "무승부"
                : state.Reason == MatchEnd.Left ? "상대가 나갔습니다"
                : won ? "승리" : "패배";
            if (_game.Local != null) _game.Local.Block(true);
        }

        public void Exit() => _game.Exit();

        public void Resume()
        {
            if (_result) return;
            _panel.SetActive(false);
            if (_game.Local != null) _game.Local.Block(false);
        }
    }
}