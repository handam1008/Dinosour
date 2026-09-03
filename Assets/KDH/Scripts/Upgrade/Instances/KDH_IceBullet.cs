using System.Collections;
using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances
{
    public class KDH_IceBullet : KDH_AbstractBulletAbility
    {
        [SerializeField] private float slowAmount = 0.2f;
        [SerializeField] private float slowDuration = 1.5f;

        [SerializeField] private UnityEvent onHitPlayer;
        
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        
        public Transform HitPoint { get; private set; }

        public KDH_Bullet Bullet { get; private set; }
        
        public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
        {
            if(collision.TryGetComponent(out ISlowable slowable))
            {
                if (bullet.IsUpgraded)
                {
                    slowable.ApplySlow(slowAmount * bullet.UpgradValue, slowDuration * bullet.UpgradValue);
                    HitPoint = collision.transform;
                    Bullet = bullet;
                    
                    onHitPlayer?.Invoke();
                    return;
                }
                
                slowable.ApplySlow(slowAmount, slowDuration);
                HitPoint = collision.transform;
                Bullet = bullet;
                
                onHitPlayer?.Invoke();
            }
        }
    }
}