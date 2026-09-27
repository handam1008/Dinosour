using UnityEngine;

namespace SSW
{
    public sealed class RoundUI : MonoBehaviour
    {
        [SerializeField] RoundLamp[] _left;
        [SerializeField] RoundLamp[] _right;
        [SerializeField] UnityEngine.UI.Text _leftScore;
        [SerializeField] UnityEngine.UI.Text _rightScore;
        bool _shown;
        byte _set;

        public void Show(MatchState state, ulong local)
        {
            bool visible = state.Round > 0 && state.Phase != MatchPhase.Intro;
            gameObject.SetActive(visible);
            if (!visible)
            {
                _shown = false;
                return;
            }
            bool first = local == state.First;
            int left = first ? state.FirstWins : state.SecondWins;
            int right = first ? state.SecondWins : state.FirstWins;
            bool animate = _shown && _set == state.Set;
            for (int i = 0; i < _left.Length; i++) _left[i].Set(i < left, animate);
            for (int i = 0; i < _right.Length; i++) _right[i].Set(i < right, animate);
            _leftScore.text = (first ? state.FirstSets : state.SecondSets).ToString();
            _rightScore.text = (first ? state.SecondSets : state.FirstSets).ToString();
            _set = state.Set;
            _shown = true;
        }
    }
}
