using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "Escape", menuName = "Player/job/Augment/Assassin/Escape", order = 0)]
    public class EscapeSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private float speedMultiplier = 0.6f; // 0.1f = 0.1배
        [SerializeField] private float duration = 2f;
        
        [Header("쿨타임 설정")]
        [SerializeField] private float cooldown = 3f;

        public override void OnHit(AugmentContext ctx)
        {
            if (!ctx.IsOnCooldown(type))
            {
                if (ctx.Owner.TryGetComponent(out ISpeedable speedable))
                {
                    speedable.ApplySpeed(speedMultiplier, duration);
                    ctx.SetCooldown(type, cooldown + duration);
                }
            }
        }
    }
}