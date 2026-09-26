using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_TouchScreenOutline : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private float knockbackForce;
        [SerializeField] private Vector2 direction;
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out IDamageable damageable))
                ApplyDamage(damageable, damage);

            if (collision.gameObject.TryGetComponent(out IForceReceiver forceReceiver))
                ApplyForce(forceReceiver, direction * knockbackForce, ForceMode2D.Impulse);
        }
        
        private void ApplyDamage(IDamageable damageable, float applyDamage)
        {
            damageable.TakeDamage(applyDamage);
        }

        private void ApplyForce(IForceReceiver forceReceiver, Vector2 applyDirection, ForceMode2D forceMode)
        {
            forceReceiver.ApplyForce(applyDirection, forceMode);
        }
    }
}
