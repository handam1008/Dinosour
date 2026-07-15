using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts
{
    public class AssassinMeleeAttack : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private Vector2 offset;
        [SerializeField] private Vector2 hitboxSize;
        [SerializeField] private float damage;
        [SerializeField] private float attackCooldown;

        private Vector2 _currentOffset;
        
        private float _currentCooldown = -99f;

        private Animator _attackAnim;
        private Animator _effectAnim;

        private void Awake()
        {
            _attackAnim = transform.Find("Visual").GetComponent<Animator>();
            _effectAnim = transform.Find("SlashEffect").GetComponent<Animator>();
            
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);

            _currentOffset = offset;
        }

        public void FaceAttack(bool isRight)
        {
            _currentOffset = isRight ? offset : new Vector2(-offset.x, offset.y);
            transform.rotation = isRight ? Quaternion.Euler(transform.rotation.x, 0f, transform.rotation.z) : Quaternion.Euler(transform.rotation.x, 180f, transform.rotation.z);
        }

        private IEnumerator AttackCoroutine()
        {
            _attackAnim.gameObject.SetActive(true);
            _effectAnim.gameObject.SetActive(true);
            _attackAnim.Play("Attack");
            _effectAnim.Play("Slash");
            yield return new WaitForSeconds(_effectAnim.GetCurrentAnimatorStateInfo(0).length);
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);
        }

        public void AssassinAttack()
        {
            if(Time.time - _currentCooldown < attackCooldown) return;
            
            _currentCooldown = Time.time;
            StartCoroutine(AttackCoroutine());
            AttackScan();
        }
        
        private void AttackScan()
        {
            Collider2D[] hits;
            hits = Physics2D.OverlapBoxAll((Vector2)transform.position + _currentOffset, hitboxSize, 0, targetMask);

            foreach (Collider2D hit in hits)
            {
                if(hit.transform.root == transform.root) continue;
                
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            
            Gizmos.DrawWireCube((Vector2)transform.position + _currentOffset, hitboxSize);
        }
    }
}