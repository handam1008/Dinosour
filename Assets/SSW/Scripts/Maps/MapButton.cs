using UnityEngine;

namespace SSW
{
    public sealed class MapButton : MonoBehaviour, IDamageable, IMapReset
    {
        [SerializeField] MonoBehaviour _target;
        [SerializeField] Transform _cap;
        [SerializeField] SpriteRenderer _light;
        [SerializeField] Color _idleColor = Color.cyan;
        [SerializeField] Color _pressedColor = Color.white;
        [SerializeField] float _cooldown = 0.5f;

        IMapAction _action;
        Vector3 _capStart;
        float _readyAt;

        public float Current => 1f;
        public float Max => 1f;

        void Awake()
        {
            _action = (IMapAction)_target;
            _capStart = _cap.localPosition;
            ResetMap();
        }

        void Update()
        {
            bool pressed = Time.time < _readyAt;
            _cap.localPosition = _capStart + (pressed ? Vector3.down * 0.08f : Vector3.zero);
            _light.color = pressed ? _pressedColor : _idleColor;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null && body.TryGetComponent<IForceReceiver>(out _))
                Press();
        }

        public void Press()
        {
            if (Time.time < _readyAt) return;
            _readyAt = Time.time + _cooldown;
            _action.Use();
        }

        public void TakeDamage(float amount)
        {
            if (amount > 0f) Press();
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            TakeDamage(amount);
        }

        public void Heal(float amount) { }

        public void ResetMap()
        {
            _readyAt = 0f;
            _cap.localPosition = _capStart;
            _light.color = _idleColor;
        }
    }
}
