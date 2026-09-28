using System.Collections;
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
        [SerializeField] Text _exitText;
        [SerializeField] RoundUI _rounds;
        [SerializeField] RoundWipe _wipe;
        [SerializeField] DraftStatus _draft;
        [SerializeField] VolumePanel _volume;
        
        [Header("sound")]
        [SerializeField] SoundCue WinSound;
        [SerializeField] SoundCue LoseSound;
        NetGame _game;
        bool _result;
        bool _exiting;

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
            _exitText.text = "나가기";
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
                        winner = player.Info.DisplayName(player.Side > 0 ? 1 : 2);
                _wipe.Show(state.Reason == MatchEnd.Draw ? "무승부" : $"{winner} 승리!");
            }
            else _wipe.Reveal();
        }

        void Update()
        {
            if (_result || _exiting || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (_panel.activeSelf) Resume();
            else Open();
        }

        public void Open()
        {
            if (_result || _exiting) return;
            _title.gameObject.SetActive(false);
            _volume.gameObject.SetActive(true);
            _panel.SetActive(true);
            if (_game.Local != null) _game.Local.Block(true);
        }

        public void ShowResult(MatchState state)
        {
            _result = true;
            _panel.SetActive(true);
            _resume.gameObject.SetActive(false);
            _volume.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            
            bool won = state.Winner == _game.LocalId;
            _title.text = state.Reason == MatchEnd.Draw ? "무승부"
                : state.Reason == MatchEnd.Left ? "상대가 나갔습니다"
                : state.Reason == MatchEnd.Surrender ? won ? "상대가 항복했습니다" : "항복했습니다"
                : won ? "승리" : "패배";
            SoundCue cue = won ? WinSound : LoseSound;
            GameAudio.Current.PlaySfx(cue);
            _exitText.text = "메인 메뉴";
            if (_game.Local != null) _game.Local.Block(true);
        }

        public void Exit()
        {
            if (_exiting) return;
            _exiting = true;
            _resume.interactable = false;
            _exit.interactable = false;
            StartCoroutine(Leave());
        }

        IEnumerator Leave()
        {
            if (!_result && _game.Match != null)
            {
                _game.Match.Surrender();
                float until = Time.realtimeSinceStartup + 2f;
                while (!_result && _game.Connected && Time.realtimeSinceStartup < until) yield return null;
            }
            yield return null;
            _game.Exit();
        }

        public void Resume()
        {
            if (_result || _exiting) return;
            _panel.SetActive(false);
            if (_game.Local != null) _game.Local.Block(false);
        }
    }
}
