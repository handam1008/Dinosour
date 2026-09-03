using SSW;
using UnityEngine;

namespace NKY.Scripts
{
    public abstract class AbstractMeleeWeapon : AbstractWeapon
    {
        [SerializeField] protected float offset;
        [SerializeField] protected Vector2 hitboxSize;
        
        protected Vector2 _currentOffset;

        public override void FaceAttack()
        {
            base.FaceAttack();
            _currentOffset = currentDirection * offset;
        }
        
        protected void AttackScan()
        {
            Collider2D[] hits;
            
            hits = Physics2D.OverlapBoxAll((Vector2)transform.position + _currentOffset, hitboxSize, _currentAngle, targetMask);
            
            foreach (Collider2D hit in hits)
            {
                if(hit.transform.root == transform.root) continue;
                
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }
    }
}