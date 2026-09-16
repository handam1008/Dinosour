using UnityEngine;

namespace SSW
{
    public sealed class DraftStatus : MonoBehaviour
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] RectTransform _content;
        [SerializeField] UnityEngine.UI.Text _label;
        [SerializeField] UnityEngine.UI.Text _time;
        [SerializeField] UnityEngine.UI.Image _back;
        NetGame _game;

        public string Label => _label.text;
        public string Time => _time.text;
        public bool Visible => _group.alpha > 0f;

        public void Bind(NetGame game)
        {
            _game = game;
            _group.alpha = 0f;
        }

        void Update()
        {
            if (_game == null || _game.State.Phase != MatchPhase.Draft || _game.Local == null)
            {
                _group.alpha = 0f;
                return;
            }
            NetDraft draft = _game.Local.Draft;
            bool waiting = draft.Ready;
            if (waiting)
                foreach (NetPlayer player in _game.Players)
                    if (!player.Draft.Ready) { draft = player.Draft; break; }
            _group.alpha = draft.Deadline > 0d ? 1f : 0f;
            _label.text = waiting ? "상대가 증강을 고르는 중..." : draft.Common ? "공용 증강 선택" : "직업 증강 선택";
            _time.text = $"{Mathf.CeilToInt(draft.Seconds)}초";
            _time.color = draft.Seconds <= 3f ? new Color(1f, 0.46f, 0.3f) : new Color(1f, 0.86f, 0.4f);
            _content.anchoredPosition = new Vector2(0f, waiting ? 90f : 220f);
            _back.enabled = waiting;
        }
    }
}
