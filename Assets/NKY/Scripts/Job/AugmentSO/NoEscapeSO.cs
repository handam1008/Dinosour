using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "NoEscape", menuName = "Player/job/Augment/Assassin/NoEscape", order = 0)]
    public class NoEscapeSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private float slowedMultiplier = 0.7f;
        [SerializeField] private float duration = 0.5f;
        
        [Header("쿨타임 설정")]
        [SerializeField] private float cooldown = 3f;
        public override void OnBasicAttackHit(AugmentContext ctx, PlayerController target)
        {
            if (!ctx.IsOnCooldown(type))
            {
                if (target.TryGetComponent(out ISlowable slowable))
                {
                    slowable.ApplySlow(slowedMultiplier , duration);
                    ctx.SetCooldown(type, cooldown + duration);
                }
            }
        } 
    }
}