using UnityEngine;

namespace SSW
{
    public sealed class MapTint : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] Color _start;
        [SerializeField] Color _end;
        [SerializeField] float _duration = 1f;
        [SerializeField] float _hold;
        [SerializeField] bool _once;
        [SerializeField] bool _playOnAwake = true;
        float _elapsed;
        bool _playing;

        void Awake() => _playing = _playOnAwake;

        public void Play()
        {
            _elapsed = 0f;
            _playing = true;
            _sprite.color = _start;
        }

        public void Stop() => _playing = false;

        void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            float period = Mathf.Max(0.01f, _duration + _hold);
            float time = _once ? _elapsed : _elapsed % period;
            float t = _duration > 0f ? Mathf.Clamp01(time / _duration) : 1f;
            _sprite.color = Color.LerpUnclamped(_start, _end, t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t));
            if (_once && t >= 1f) _playing = false;
        }
    }
}