using UnityEngine;

namespace SSW
{
    public sealed class Pin : MonoBehaviour, IDamageable, IDamageReceiver, IMapReset
    {
        [SerializeField] MonoBehaviour _target;
        [SerializeField] Collider2D _hit;
        [SerializeField] SpriteRenderer _face;

        IMapAction _action;
        float _health;

        public float Current => _health;
        public float Max => 1f;

        void Awake()
        {
            _action = (IMapAction)_target;
            ResetMap();
        }

        public void TakeDamage(float amount)
        {
            ReceiveDamage(new DamageRequest(null, amount));
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            ReceiveDamage(new DamageRequest(null, amount, DamageTag.None, isCritical));
        }

        public DamageResult ReceiveDamage(DamageRequest request)
        {
            float damage = Mathf.Max(0f, request.Amount);
            float applied = Mathf.Min(_health, damage);
            _health -= applied;
            bool cut = applied > 0f && _health <= 0f;
            if (cut)
            {
                _hit.enabled = false;
                _face.enabled = false;
                _action.Use();
            }
            return new DamageResult(damage, applied, cut);
        }

        public void Heal(float amount)
        {
            if (_health > 0f) _health = Mathf.Min(Max, _health + Mathf.Max(0f, amount));
        }

        public void ResetMap()
        {
            _health = Max;
            _hit.enabled = true;
            _face.enabled = true;
        }
    }
}
