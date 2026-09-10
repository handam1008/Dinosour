using System;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_BreatheDamageCaster : MonoBehaviour
    {
        [SerializeField] private float tick;
        [SerializeField] private float damage;
        private Collider2D _collider;
        private float _timer;
        private bool _canDamage;
        
        private void Update()
        {
            if (_canDamage)
            {
                _timer += Time.deltaTime;

                if (_timer >= tick)
                {
                    if (_collider != null)
                    {
                        ApplyDamage(_collider, damage);
                        _timer = 0;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamageable _))
            {
                _collider =  collision;
                _canDamage = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamageable _))
            {
                _collider = null;
                _canDamage = false;
            }
        }

        private void ApplyDamage(Collider2D collision, float applyDaamge)
        {
            if (collision.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(applyDaamge);
        }
    }
}
