using System;
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

        private Animator _attackAnim;
        private Animator _effectAnim;

        private void Awake()
        {
            _attackAnim = transform.Find("Visual").GetComponent<Animator>();
            _effectAnim = transform.Find("SlashEffect").GetComponent<Animator>();
            
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);
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
            StartCoroutine(AttackCoroutine());
            AttackScan();
        }
        
        private void AttackScan()
        {
            Collider2D[] hits;
            hits = Physics2D.OverlapBoxAll((Vector2)transform.position + offset, hitboxSize, 0, targetMask);

            foreach (Collider2D hit in hits)
            {
                if(hit.gameObject == gameObject) return;
                
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            
            Gizmos.DrawWireCube((Vector2)transform.position + offset, hitboxSize);
        }
    }
}