using UnityEngine;

namespace NKY.Scripts
{
    public class AssassinNormalSkill : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private float speed;
        [SerializeField] private LayerMask whatIsTarget;
        [SerializeField] private float destroyTime;

        private bool _isThrow;
        private float _curTime;
        
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _isThrow = true;
            if (_rb != null)
            {
                _rb.linearVelocity = transform.up * speed;
            }
        }

        private void FixedUpdate()
        {
            _curTime += Time.fixedDeltaTime;
            if (_curTime >= destroyTime)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((whatIsTarget.value & (1 << other.gameObject.layer)) != 0)
            {
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
            
                _isThrow = false;

                if (other.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }
    }
}