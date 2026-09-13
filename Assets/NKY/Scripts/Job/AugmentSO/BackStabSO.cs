using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "BackStab", menuName = "Player/job/Augment/Assassin/BackStab", order = 0)]
    public class BackStabSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")] 
        [SerializeField] private float backStabAngle = 120f; //후방 120도
        [SerializeField] private float attackMultiplier = 1.4f;

        public override void OnBasicAttackHit(AugmentContext ctx, PlayerController target)
        {
            if (target == null) return;
            
            if (IsBackAttack(ctx, target))
            {
                ctx.SetFlag(type, true);
            }
        }

        public override float ModifyDamage(AugmentContext ctx, float currentDamage, bool isBasicAttack)
        {
            if (ctx.GetFlag(type))
            {
                ctx.SetFlag(type, false);
                return currentDamage * attackMultiplier;
            }

            return currentDamage;
        }

        private bool IsBackAttack(AugmentContext ctx, PlayerController target)
        {
            // 1. 타겟 scale.x 기반 정면 벡터
            Vector2 targetForward = target.FacingSign > 0 ? Vector2.right : Vector2.left;

            // 2. 공격자 방향 벡터
            Vector2 dirToAttacker = ((Vector2)ctx.Owner.transform.position - (Vector2)target.transform.position).normalized;

            // 3. 내적 계산
            float dot = Vector2.Dot(targetForward, dirToAttacker);

            // 4. 후방 범위 임계값 계산 (각도가 120도일 때 정면 기준 120°~240° 영역 탐색)
            float thresholdAngle = 180f - (backStabAngle * 0.5f);
            float thresholdDot = Mathf.Cos(thresholdAngle * Mathf.Deg2Rad);
            
            return dot < thresholdDot;
        }
    }
}