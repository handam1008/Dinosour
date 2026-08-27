using SSW;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_IceBullet : KDH_AbstractBullet
    {
        [SerializeField] private float applySlow = 0.2f;
        [SerializeField] private float durationTime = 1.5f;
        
        public override void Ability(Collider2D collision)
        {
            ISlowable slowable =
                collision.GetComponentInParent<ISlowable>();
            
            if (slowable == null)
                return;
            
            slowable.ApplySlow(applySlow, durationTime);
        }
    }
}