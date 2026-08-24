using Cysharp.Threading.Tasks;
using UnityEngine;

using SSW;

namespace NKY.Scripts
{
    public class AssassinNormalSkill : MonoBehaviour
    { 
        private float _damage;
        private float _speed;
        private LayerMask _whatIsTarget;
        private float _destroyTime;
        
        private float _curTime;
        
        public Rigidbody2D Rb { get;  private set; }

        private TestJump _giver;

        public void Init(TestJump giver, float damage,  float speed, LayerMask whatIsTarget, float destroyTime)
        {
            _giver = giver;
            _damage = damage;
            _speed = speed;
            _whatIsTarget = whatIsTarget;
            _destroyTime = destroyTime;
        }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (Rb != null)
            {
                Rb.linearVelocity = transform.up * _speed;
            }
        }

        private void FixedUpdate()
        {
            _curTime += Time.fixedDeltaTime;
            if(Rb.linearVelocity.magnitude > 0)
                transform.up = Rb.linearVelocity;
            if (_curTime >= _destroyTime)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_whatIsTarget.value & (1 << other.gameObject.layer)) != 0)
            {
                if (Rb != null)
                {
                    Rb.linearVelocity = Vector2.zero;
                    Rb.gravityScale = 0;
                }
            }
            
            if (other.gameObject != _giver.gameObject && other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
            }
        }
    }
}