using SSW;
using Unity.Netcode;
using UnityEngine;

namespace NKY.Scripts.Skill
{
    public class AssassinNormalSkill : NetworkBehaviour
    { 
        [SerializeField] private int bounceCount = 0;
        private float _damage;
        private float _speed;
        private LayerMask _whatIsTarget;
        private float _destroyTime;
        private float _curTime;

        private PlayerController _giver;
        public Rigidbody2D Rb { get; private set; }
        
        private int maxBounceCount;
        private int currentBounceCount;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
        }

        public void Init(PlayerController giver, float damage, float speed, LayerMask whatIsTarget, float destroyTime)
        {
            _giver = giver;
            _damage = damage;
            _speed = speed;
            _whatIsTarget = whatIsTarget;
            _destroyTime = destroyTime;
            
            currentBounceCount = bounceCount;
        }

        public void Launch(Vector2 direction)
        {
            if (!IsServer)
                return;

            Rb.linearVelocity = direction * _speed;
        }

        private void FixedUpdate()
        {
            if (!IsServer)
                return;

            _curTime += Time.fixedDeltaTime;

            if (Rb != null && Rb.linearVelocity.sqrMagnitude > 0.01f)
                transform.up = Rb.linearVelocity;

            if (_curTime >= _destroyTime)
            {
                NetworkObject.Despawn();
            }
        }
        
        public void SetMaxBounces(int maxBounce)
        {
            maxBounceCount = maxBounce;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer)
                return;
            
            if ((_whatIsTarget.value & (1 << other.gameObject.layer)) != 0)
            {
                if (maxBounceCount > currentBounceCount)
                {
                    if (TryBounce())
                    {
                        currentBounceCount++;
                        _curTime = Mathf.Max(0f, _curTime - 1.5f);
                        return;
                    }
                }
                
                if (Rb != null)
                {
                    Rb.linearVelocity = Vector2.zero;
                    Rb.gravityScale = 0;
                    currentBounceCount = 0;
                }
            }

            if (_giver != null && other.gameObject != _giver.gameObject && other.TryGetComponent(out IDamageable damageable))
            {
                CombatDamage.Deal(_giver, damageable, _damage, DamageTag.JobSkill | DamageTag.Projectile);
            }
        }

        private bool TryBounce()
        {
            Vector2 rayOrigin = (Vector2)transform.position - (Vector2)transform.up * 0.3f;
            RaycastHit2D target = Physics2D.Raycast(rayOrigin, transform.up, 1.2f, _whatIsTarget.value);
            
            if (target.collider == null || target.normal == Vector2.zero) return false;
            
            Vector2 inDirection = transform.up; 
            Vector2 inNormal = target.normal;
            Vector2 reflectDirection = Vector2.Reflect(inDirection, inNormal);

            transform.up = reflectDirection;
            if (Rb != null)
            {
                Rb.linearVelocity = reflectDirection * _speed;
            }
            
            transform.position = target.point + (inNormal * 0.1f);

            return true;
        }
    }
}