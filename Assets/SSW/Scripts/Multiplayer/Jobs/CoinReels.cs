using System;
using JJW.Script.Jackpot;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class CoinReels : MonoBehaviour
    {
        [Serializable] sealed class Reel
        {
            public Image Current;
            public Image Next;
            public float Stop;
        }

        static readonly int[] Winning = { -1, 1, 0, 2, 3, 4, 5 };
        static readonly int[][] Misses = { new[] { 0, 1, 2 }, new[] { 3, 0, 1 }, new[] { 4, 2, 5 }, new[] { 1, 5, 0 }, new[] { 2, 3, 4 } };
        [SerializeField] CanvasGroup _group;
        [SerializeField] Reel[] _reels;
        [SerializeField] Sprite[] _symbols;
        [SerializeField] float _step = 0.075f;
        [SerializeField] float _hold = 0.55f;
        [SerializeField] float _fade = 0.12f;
        [SerializeField] float _travel = 170f;
        [SerializeField] AudioClip _stop;
        [SerializeField] float _volume = 0.7f;
        double _started;
        uint _roll;
        JackpotResultType _result;
        bool _active;
        Transform _view;

        public float Duration => _reels[2].Stop;
        public float Interval => Duration + _hold + _fade;
        public bool Spinning => _active && Stops != 7;
        public int Stops { get; private set; }
        public Vector3Int Symbols { get; private set; }
        public float Alpha => _group.alpha;

        public void SetView(Transform view) => _view = view;

        public void Play(JackpotResultType result, uint roll, double started)
        {
            _result = result;
            _roll = roll;
            _started = started;
            _active = true;
            Stops = 0;
        }

        public void Tick(double now)
        {
            if (!_active) return;
            transform.rotation = _view.rotation;
            float age = Mathf.Max(0f, (float)(now - _started));
            if (age >= Interval) { Clear(); return; }
            _group.alpha = Mathf.Min(Mathf.Clamp01(age / _fade), Mathf.Clamp01((Interval - age) / _fade));
            Vector3Int shown = default;
            for (int i = 0; i < _reels.Length; i++)
            {
                Reel reel = _reels[i];
                int final = Winning[(int)_result];
                if (final < 0) final = Misses[_roll % (uint)Misses.Length][i];
                float last = reel.Stop - _step;
                bool stopped = age >= reel.Stop;
                int current;
                int next;
                float progress;
                if (stopped)
                {
                    current = next = final;
                    progress = 0f;
                    if ((Stops & (1 << i)) == 0)
                    {
                        Stops |= 1 << i;
                        if (age - reel.Stop < 0.2f) GameAudio.GetOrCreate().PlaySfx(_stop, _volume);
                    }
                }
                else if (age >= last)
                {
                    current = Symbol(i, Mathf.FloorToInt(last / _step));
                    next = final;
                    progress = (age - last) / _step;
                }
                else
                {
                    float position = age / _step;
                    int frame = Mathf.FloorToInt(position);
                    current = Symbol(i, frame);
                    next = Symbol(i, frame + 1);
                    progress = position - frame;
                }
                reel.Current.sprite = _symbols[current];
                reel.Next.sprite = _symbols[next];
                float punch = stopped && age < reel.Stop + 0.16f ? -12f * Mathf.Sin((age - reel.Stop) / 0.16f * Mathf.PI) : 0f;
                reel.Current.rectTransform.anchoredPosition = new Vector2(0f, -_travel * progress + punch);
                reel.Next.rectTransform.anchoredPosition = new Vector2(0f, _travel * (1f - progress));
                shown[i] = current;
            }
            Symbols = shown;
        }

        int Symbol(int reel, int frame)
        {
            uint value = unchecked(_roll * 747796405u + (uint)frame * 2891336453u + (uint)reel * 277803737u);
            value ^= value >> 16;
            return (int)(value % (uint)_symbols.Length);
        }

        public void Clear()
        {
            _active = false;
            _group.alpha = 0f;
        }
    }
}
