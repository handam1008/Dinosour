using UnityEngine;

namespace SSW
{
    /// <summary>
    /// Shared outgoing-damage entry point. Job attacks remain independent from status implementations.
    /// </summary>
    public static class CombatDamage
    {
        public static bool TryDeal(Component source, IDamageable target, float baseAmount)
        {
            return TryDeal(source, target, baseAmount, false);
        }

        public static bool TryDeal(Component source, IDamageable target, float baseAmount, bool isCritical)
        {
            if (target == null || baseAmount <= 0f)
                return false;

            target.TakeDamage(Resolve(source, baseAmount), isCritical);
            return true;
        }

        public static float Resolve(Component source, float baseAmount)
        {
            float amount = Mathf.Max(0f, baseAmount);
            if (source == null)
                return amount;

            IOutgoingDamageModifier modifier = source.GetComponentInParent<IOutgoingDamageModifier>();
            return modifier != null ? modifier.ModifyOutgoingDamage(amount) : amount;
        }
    }
}
