using SSW;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NKY.Scripts
{
    public class KHG_nomalAttack : MonoBehaviour
    {
        [Header("애니메이션 설정")]
        [SerializeField] private Animator swordAnimator;
        [SerializeField] private string attackTriggerName = "Attack";

        [Header("공격 설정")]
        [SerializeField] private float damage = 30f;
        [SerializeField] private float attackDuration = 0.1f;
        [SerializeField] private float attackCooldown = 0.3f;

        [Header("공격 콜라이더")]
        [SerializeField] private Collider2D attackCollider;

        private float lastAttackTime = -999f;
        private bool isAttacking;

        private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        public System.Action<GameObject, float> OnNormalAttackHit;

        private void Awake()
        {
            // Animator는 자동으로 찾지 않음.
            // Inspector에서 평타 전용 Animator를 직접 넣어주세요.

            if (attackCollider == null)
                attackCollider = GetComponent<Collider2D>();

            if (attackCollider != null)
                attackCollider.enabled = false;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 공격 쿨타임
                if (Time.time - lastAttackTime < attackCooldown)
                    return;

                // 공격 시작
                lastAttackTime = Time.time;

                StartCoroutine(NormalAttack());
            }
        }

        private IEnumerator NormalAttack()
        {
            isAttacking = true;

            // 평타 Animator만 실행
            if (swordAnimator != null)
            {
                swordAnimator.SetTrigger(attackTriggerName);
            }
            else
            {
                Debug.LogWarning(
                    "KHG_nomalAttack의 Sword Animator가 지정되지 않았습니다."
                );
            }

            hitTargets.Clear();

            if (attackCollider != null)
                attackCollider.enabled = true;

            yield return new WaitForSeconds(attackDuration);

            if (attackCollider != null)
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

            // 같은 공격에 같은 오브젝트가 여러 번 맞지 않게 함
            if (hitTargets.Contains(targetObject))
                return;

            hitTargets.Add(targetObject);

            DamageResult result =
                CombatDamage.Deal(
                    this,
                    target,
                    damage,
                    DamageTag.BasicAttack
                );

            OnNormalAttackHit?.Invoke(
                targetObject,
                result.AppliedAmount
            );
        }
    }
}