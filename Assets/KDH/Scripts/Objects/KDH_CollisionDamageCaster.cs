using System;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_CollisionDamageCaster : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private float knockbackForce;
        private Vector2 _direction;
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 direction = (
                collision.transform.position - transform.position
            ).normalized;

            if (collision.gameObject.TryGetComponent(out IDamageable damageable))
                ApplyDamage(damageable, damage);

            if (collision.gameObject.TryGetComponent(out IForceReceiver forceReceiver))
                ApplyForce(forceReceiver, direction * knockbackForce, ForceMode2D.Impulse);
        }
        
        private void ApplyDamage(IDamageable damageable, float applyDamage)
        {
            damageable.TakeDamage(applyDamage);
        }

        private void ApplyForce(IForceReceiver forceReceiver, Vector2 direction, ForceMode2D forceMode)
        {
            forceReceiver.ApplyForce(direction, forceMode);
        }
    }
}
