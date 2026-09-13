using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "Lure", menuName = "Player/job/Augment/Assassin/Lure", order = 0)]
    public class LureSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private float damageMultiplier = 1.2f; // 0.1 = 10%

        public override void OnBasicAttackHit(AugmentContext ctx, PlayerController target)
        {
            if (target.TryGetComponent(out Health health))
            {
                if (Mathf.Approximately(health.Current, health.Max))
                {
                    ctx.SetFlag(type, true);
                }
            }
        }

        public override float ModifyDamage(AugmentContext ctx, DamageInfo info)
        {
            if (info.IsBasicAttack && ctx.GetFlag(type))
            {
                ctx.SetFlag(type, false);
                
                return info.Damage * damageMultiplier;
            }

            return info.Damage;
        }
    }
}