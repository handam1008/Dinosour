using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_GravityBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")] 
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        [SerializeField] private float forceAmount = 4f;
    
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        
        [SerializeField] private UnityEvent onHitPlayer;

        private SoundCue _gravitySound;
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            HitPoint = collision.transform;
            Bullet = bullet;

            // if (_gravitySound == null)
            //     _gravitySound = Bullet.PlayerGun.SoundCues.list[7];
            //
            // if (_gravitySound  != null)
            //     NetGame.Current.Sounds.Play(_gravitySound);
            
            if (Bullet.IsUpgraded)
            {
                IForceReceiver forceReceiver =
                    HitPoint.GetComponentInParent<IForceReceiver>();

                if (forceReceiver != null)
                {
                    Vector2 direction =
                        (HitPoint.transform.position - transform.position).normalized;

                    forceReceiver.ApplyForce(
                        direction * forceAmount * Bullet.UpgradValue,
                        ForceMode2D.Impulse);
                }
            }
            else
            {
                IForceReceiver forceReceiver =
                    HitPoint.GetComponentInParent<IForceReceiver>();

                if (forceReceiver != null)
                {
                    Vector2 direction =
                        (HitPoint.transform.position - transform.position).normalized;

                    forceReceiver.ApplyForce(
                        direction * forceAmount,
                        ForceMode2D.Impulse);
                }
            }
            
            onHitPlayer.Invoke();
        }
    }
}
