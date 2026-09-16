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
        [SerializeField] RoundUI _rounds;
        [SerializeField] RoundWipe _wipe;
        [SerializeField] DraftStatus _draft;
        NetGame _game;
        bool _result;

        internal bool IsOpen => _panel.activeSelf;
        internal string Title => _title.gameObject.activeSelf ? _title.text : string.Empty;
        internal bool CanResume => _resume.gameObject.activeSelf;
        internal DraftStatus Draft => _draft;
        internal RoundWipe Wipe => _wipe;

        public void Bind(NetGame game)
        {
            _game = game;
            _resume.onClick.AddListener(Resume);
            _exit.onClick.AddListener(Exit);
            _panel.SetActive(false);
            _rounds.Show(default, game.LocalId);
            _draft.Bind(game);
            _wipe.Hide();
        }

        public void ShowRound(MatchState state)
        {
            _rounds.Show(state, _game.LocalId);
            if (state.Phase == MatchPhase.RoundEnd || state.Phase == MatchPhase.SetEnd)
            {
                string winner = "";
                foreach (NetPlayer player in _game.Players)
                    if (player.OwnerClientId == state.Winner)
                        winner = player.Info.Name.IsEmpty ? $"플레이어 {(player.Side > 0 ? 1 : 2)}" : player.Info.Name.ToString();
                _wipe.Show(state.Reason == MatchEnd.Draw ? "무승부" : $"{winner} 승리!");
            }
            else _wipe.Reveal();
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
