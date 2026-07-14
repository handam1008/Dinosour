using UnityEngine;

namespace SSW
{
    public class VanishingPlatform : MonoBehaviour
    {
        [SerializeField] float _visibleDuration = 3f;
        [SerializeField] float _hiddenDuration = 1.5f;
        [SerializeField] float _warningDuration = 0.8f;
        [SerializeField] float _startOffset;

        SpriteRenderer _renderer;
        Collider2D _collider;
        Color _baseColor;
        float _timer;
        bool _visible = true;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _baseColor = _renderer.color;
            _timer = _visibleDuration + _startOffset;
        }

        void Update()
        {
            _timer -= Time.deltaTime;

            if (_visible && _timer <= _warningDuration)
            {
                Color c = _baseColor;
                c.a = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f ? 0.3f : 1f;
                _renderer.color = c;
            }

            if (_timer > 0f) return;

            _visible = !_visible;
            _timer = _visible ? _visibleDuration : _hiddenDuration;
            _renderer.enabled = _visible;
            _collider.enabled = _visible;
            _renderer.color = _baseColor;
        }
    }
}
