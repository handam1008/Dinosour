using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class MapTempo : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] float _delay = 10f;
        [SerializeField, Min(0.1f)] float _duration = 5f;
        [SerializeField] float _minimum = 0.5f;
        [SerializeField] float _maximum = 1f;
        [SerializeField] SpriteRenderer _clock;
        [SerializeField] float _clockSize = 10f;
        [SerializeField] MapTint _startTint;
        [SerializeField] MapTint _endTint;
        bool _active;
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1);

        void Update()
        {
            if (!IsSpawned) return;
            if (!NetGame.Current.CanFight)
            {
                Time.timeScale = 1f;
                if (_active) SetActive(false);
                if (IsServer && _startedAt.Value >= 0) _startedAt.Value = -1;
                return;
            }
            if (IsServer && _startedAt.Value < 0) _startedAt.Value = NetworkManager.ServerTime.Time;
            if (_startedAt.Value < 0) return;
            double age = NetworkManager.ServerTime.Time - _startedAt.Value;
            float phase = (float)(System.Math.Max(0, age) % (_delay + _duration));
            bool active = phase >= _delay;
            if (_active != active) SetActive(active);
            float progress = active ? (phase - _delay) / _duration : 0f;
            Time.timeScale = active ? Mathf.Clamp(Mathf.Lerp(_minimum, _maximum, progress), 0.1f, 2f) : 1f;
            float pulse = active ? Mathf.Clamp01((phase - _delay) / 1.5f) : 1f;
            _clock.transform.localScale = Vector3.one * (_clockSize * pulse);
            Color color = _clock.color;
            color.a = Mathf.Lerp(0.2f, 0f, pulse);
            _clock.color = color;
        }

        void SetActive(bool active)
        {
            _active = active;
            _startTint.Stop();
            _endTint.Stop();
            if (active) _startTint.Play();
            else _endTint.Play();
        }

        void OnDisable() => Time.timeScale = 1f;
        public override void OnNetworkDespawn() => Time.timeScale = 1f;
    }
}
