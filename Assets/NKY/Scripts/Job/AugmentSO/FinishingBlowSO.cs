using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "FinishingBlow", menuName = "Player/job/Augment/Assassin/FinishingBlow", order = 0)]
    public class FinishingBlowSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private float damageMultiplier = 1.15f;
        
        public override void OnSkillUsed(AugmentContext ctx)
        {
            ctx.SetFlag(type, true);
        }
        
        public override float ModifyDamage(AugmentContext ctx, DamageInfo info)
        {
            // 기본 공격이고 + 스킬을 써서 플래그가 켜진 상태라면
            if (info.IsBasicAttack && ctx.GetFlag(type))
            {
                // 1회성 적용이므로 플래그를 소모(끄기)
                ctx.SetFlag(type, false);
                
                // 데미지 1.15배 증폭
                return info.Damage * damageMultiplier;
            }
            return info.Damage;
        }
    }
}