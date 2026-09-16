using System.Collections;
using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_FireBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")]
        [SerializeField] private int dotDamage = 5;
        [SerializeField] private int dotDamageCount = 6;
        [field: SerializeField] public float FireireDuration { get; private set; }= 3f;
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
    
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        [SerializeField] private UnityEvent onHitPlayer;
        
        public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
        {
            if (bullet.IsUpgraded)
            {
                StartCoroutine(TakeDamageDelay(collision, dotDamage * bullet.UpgradValue, FireireDuration * bullet.UpgradValue));
                HitPoint = collision.transform;
                Bullet = bullet;
                
                onHitPlayer?.Invoke();
                return;
            }
            
            StartCoroutine(TakeDamageDelay(collision, dotDamage, FireireDuration));
            HitPoint = collision.transform;
            Bullet = bullet;
            
            onHitPlayer?.Invoke();
        }

        private IEnumerator TakeDamageDelay(Collider2D collision, float damage, float duration)
        {
            for (int i = 0; i < dotDamageCount; i++)
            {
                yield return new WaitForSeconds(FireireDuration / dotDamageCount);
                if (collision.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }
    }
}
