using UnityEngine;

namespace SSW
{
    public class FlyingCard : MonoBehaviour
    {
        [SerializeField] float _baseDamage = 5f;
        [SerializeField] float _knockbackForce = 4f;

        [SerializeField] float _spadeDamagePerNumber = 1f;

        [SerializeField] float _heartHealPerNumber = 1f;

        [SerializeField] float _diamondDamagePerNumber = 1f;
        [SerializeField] float _diamondRadius = 1.5f;

        [SerializeField] float _cloverSlowPerNumber = 0.05f;
        [SerializeField] float _cloverSlowDuration = 1.5f;

        Suit _suit;
        int _number;
        Health _casterHealth;

        public void Configure(Suit suit, int number, Health casterHealth)
        {
            _suit = suit;
            _number = number;
            _casterHealth = casterHealth;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = _baseDamage + (_suit == Suit.Spade ? _number * _spadeDamagePerNumber : 0f);
                damageable.TakeDamage(damage);
            }

            if (_suit == Suit.Clover)
            {
                ISlowable slowable = other.GetComponent<ISlowable>();
                if (slowable != null) slowable.ApplySlow(_number * _cloverSlowPerNumber, _cloverSlowDuration);
            }

            if (_suit == Suit.Heart && _casterHealth != null)
                _casterHealth.Heal(_number * _heartHealPerNumber);

            if (_suit == Suit.Diamond)
                Explode();

            Rigidbody2D targetRb = other.attachedRigidbody;
            Rigidbody2D selfRb = GetComponent<Rigidbody2D>();
            if (targetRb != null && selfRb != null)
                targetRb.AddForce(selfRb.linearVelocity.normalized * _knockbackForce, ForceMode2D.Impulse);

            Destroy(gameObject);
        }

        void Explode()
        {
            float damage = _number * _diamondDamagePerNumber;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _diamondRadius);
            foreach (Collider2D hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null) damageable.TakeDamage(damage);
            }
        }
    }
}
