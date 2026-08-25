using UnityEngine;

namespace NKY.Scripts
{
    public abstract class AbstractWeapon : MonoBehaviour
    {
        [SerializeField] protected LayerMask targetMask;
        [SerializeField] protected float damage;
        [SerializeField] protected float attackCooldown;
        
        protected float _currentCooldown = -99f;

        protected Animator _attackAnim;
        protected Animator _effectAnim;

        protected float _currentAngle;
        
        protected bool isAttacking = false;
        public virtual void FaceAttack(Vector2 direction)
        {
            _currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}