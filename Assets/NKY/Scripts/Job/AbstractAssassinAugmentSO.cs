using SSW;
using UnityEngine;

namespace NKY.Scripts.Job
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "MENUNAME", order = 0)]
    public abstract class AbstractAssassinAugmentSO : AssassinAugment
    {
        // 증강들이 필요에 따라 재정의(override)하여 사용할 훅(Hook)들
        public virtual float ModifyDamage(AugmentContext ctx, float currentDamage, bool isBasicAttack) => currentDamage;
        public virtual void OnBasicAttackHit(AugmentContext ctx, PlayerController target) {}
        public virtual void OnSkillUsed(AugmentContext ctx) {}
        public virtual void OnSpawnProjectile(AugmentContext ctx, GameObject projectile) {}
        public virtual void OnHit(AugmentContext ctx) {}
        public virtual void OnHealthChanged(AugmentContext ctx, float currentHp, float maxHp) {}
        public virtual void OnRoundStart(AugmentContext ctx) {}
    }
}