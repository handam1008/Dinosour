using UnityEngine;
using UnityEngine.InputSystem;

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
        
        protected Vector2 currentDirection;
        
        protected Camera _cam;

        protected virtual void Awake()
        {
            _cam = Camera.main;
        }

        protected virtual void Update()
        {
            FaceAttack();
        }

        public virtual void FaceAttack()
        {
            Vector3 mouse = Mouse.current.position.ReadValue();
            mouse.z = -_cam.transform.position.z;

            Vector3 world = _cam.ScreenToWorldPoint(mouse);
            world.z = transform.position.z;

            Vector3 diff = world - transform.position;
        
            currentDirection = diff.normalized;
            
            _currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        }
        
        public virtual void Attack()
        {
            
        }
    }
}