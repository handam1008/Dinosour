using SSW;
using UnityEngine;

namespace KDH.Scripts.Upgrade.Instances
{
    public class KDH_IceBullet : KDH_AbstractBulletAbility
    {
        [SerializeField] private float slowAmount = 0.2f;
        [SerializeField] private float slowDuration = 1.5f;
        
        public override void BulletAbility(Collider2D collision)
        {
            if(!collision.TryGetComponent(out ISlowable slowable)) return;
            
            slowable.ApplySlow(slowAmount, slowDuration);
            Debug.Log(slowable);
        }
    }
}