using UnityEngine;

namespace SSW
{
    public class FlyingCard : MonoBehaviour
    {
        [SerializeField] float _damage = 10f;
        [SerializeField] float _knockbackForce = 4f;

        void OnTriggerEnter2D(Collider2D other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(_damage);

            Rigidbody2D targetRb = other.attachedRigidbody;
            Rigidbody2D selfRb = GetComponent<Rigidbody2D>();
            if (targetRb != null && selfRb != null)
                targetRb.AddForce(selfRb.linearVelocity.normalized * _knockbackForce, ForceMode2D.Impulse);

            Destroy(gameObject);
        }
    }
}
