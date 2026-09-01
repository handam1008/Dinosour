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
        
        public Transform HitPoint { get; private set; }

        public KDH_Bullet Bullet { get; private set; }
        
        public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
        {
            if(!collision.TryGetComponent(out ISlowable slowable)) return;

            if (bullet.IsUpgraded)
            {
                slowable.ApplySlow(slowAmount * bullet.UpgradValue, slowDuration * bullet.UpgradValue);
                HitPoint = collision.transform;
                Bullet = bullet;
                
                onHitPlayer?.Invoke();
                Debug.Log("Upgraded: " + slowable);
                return;
            }
            
            slowable.ApplySlow(slowAmount * bullet.UpgradValue, slowDuration * bullet.UpgradValue);
            HitPoint = collision.transform;
            Bullet = bullet;
            
            onHitPlayer?.Invoke();
            
            Debug.Log(slowable);
        }
    }
}