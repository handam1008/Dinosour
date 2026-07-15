using UnityEngine;

using SSW;
using UnityEditor.VersionControl;

namespace NKY.Scripts
{
    public class AssassinNormalSkill : MonoBehaviour
    { 
        private float _damage;
        private float _speed;
        private LayerMask _whatIsTarget;
        private float _destroyTime;
        private Task
        
        private float _curTime;
        
        private Rigidbody2D _rb;

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
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = transform.up * _speed;
            }
        }

        private void FixedUpdate()
        {
            _curTime += Time.fixedDeltaTime;
            if(_rb.linearVelocity.magnitude > 0)
                transform.up = _rb.linearVelocity;
            if (_curTime >= _destroyTime)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_whatIsTarget.value & (1 << other.gameObject.layer)) != 0)
            {
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector2.zero;
                    _rb.gravityScale = 0;
                }
            }
            
            if (other.gameObject != _giver.gameObject && other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
            }
        }
    }
}