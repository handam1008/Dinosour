using UnityEngine;

namespace SSW
{
    public sealed class BreakTile : MonoBehaviour, IDamageable, IDamageReceiver, IMapReset
    {
        [SerializeField] BoxCollider2D _solid;
        [SerializeField] GameObject _visual;
        [SerializeField] Transform _meter;
        [SerializeField] float _health = 10f;
        [SerializeField] float _restoreDelay = 7f;

        float _current;
        float _restoreAt;

        public float Current => _current;
        public float Max => _health;
        public bool IsBroken => _current <= 0f;

        void Awake()
        {
            ResetMap();
        }

        void Update()
        {
            if (IsBroken && Time.time >= _restoreAt && !MapSpace.Occupied(_solid))
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
            float amount = Mathf.Max(0f, request.Amount);
            float applied = Mathf.Min(_current, amount);
            _current -= applied;
            _meter.localScale = new Vector3(_current / _health, 1f, 1f);
            bool broken = applied > 0f && IsBroken;
            if (broken)
            {
                _solid.enabled = false;
                _visual.SetActive(false);
                _restoreAt = Time.time + _restoreDelay;
            }

            return new DamageResult(amount, applied, broken);
        }

        public void Heal(float amount)
        {
            if (IsBroken) return;
            _current = Mathf.Clamp(_current + Mathf.Max(0f, amount), 0f, _health);
            _meter.localScale = new Vector3(_current / _health, 1f, 1f);
        }

        public void ResetMap()
        {
            _current = _health;
            _meter.localScale = Vector3.one;
            _solid.enabled = true;
            _visual.SetActive(true);
        }
    }
}
