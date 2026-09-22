using SSW;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NKY.Scripts
{
    public class KHG_nomalAttack : MonoBehaviour
    {
        [Header("공격 설정")] [SerializeField] private float damage = 30f;
        [SerializeField] private float attackDuration = 0.1f;
        [SerializeField] private float attackCooldown = 0.3f;

        [Header("공격 콜라이더")] [SerializeField] private Collider2D attackCollider;

        private float lastAttackTime = -999f;
        private bool isAttacking;

        private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        public System.Action<GameObject, float> OnNormalAttackHit;

        private void Awake()
        {
            if (attackCollider == null)
                attackCollider = GetComponent<Collider2D>();

            if (attackCollider != null)
                attackCollider.enabled = false;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Time.time - lastAttackTime < attackCooldown)
                    return;

                lastAttackTime = Time.time;

                StartCoroutine(NormalAttack());
            }
        }

        private IEnumerator NormalAttack()
        {
            isAttacking = true;

            hitTargets.Clear();

            attackCollider.enabled = true;

            yield return new WaitForSeconds(attackDuration);

            attackCollider.enabled = false;

            isAttacking = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isAttacking)
                return;

            IDamageable target =
                other.GetComponentInParent<IDamageable>();

            if (target == null)
                return;

            GameObject targetObject = other.gameObject;

            if (hitTargets.Contains(targetObject))
                return;

            hitTargets.Add(targetObject);

            target.TakeDamage(damage);

            OnNormalAttackHit?.Invoke(targetObject, damage);
        }

    }
}    