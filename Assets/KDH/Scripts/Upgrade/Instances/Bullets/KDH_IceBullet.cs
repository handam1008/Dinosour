using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_IceBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")]
        [SerializeField] private float slowAmount = 0.2f;
        [SerializeField] private float slowDuration = 1.5f;
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        
        [SerializeField] private UnityEvent onHitPlayer;

        public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
        {
            if(collision.TryGetComponent(out ISlowable slowable))
            {
                Bullet = bullet;
                HitPoint = collision.transform;
                onHitPlayer?.Invoke(); // 이게 효과 실행인 듯?
                
                if (bullet.IsUpgraded)
                {
                    slowable.ApplySlow(slowAmount * bullet.UpgradValue, slowDuration * bullet.UpgradValue);
                    return;
                }
                
                slowable.ApplySlow(slowAmount, slowDuration);
            }
        }
    }
}