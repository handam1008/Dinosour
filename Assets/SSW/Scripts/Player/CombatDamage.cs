using System;
using System.Collections.Generic;
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

            Deal(source, target, baseAmount, DamageTag.None, isCritical);
            return true;
        }

        public static DamageResult Deal(
            Component source,
            IDamageable target,
            float baseAmount,
            DamageTag tags = DamageTag.None,
            bool isCritical = false)
        {
            if (target == null || baseAmount <= 0f)
                return new DamageResult(0f, 0f, false);

            float amount = Resolve(source, baseAmount);
            DamageRequest request = new DamageRequest(source, amount, tags, isCritical);
            DamageResult result;

            if (target is IDamageReceiver receiver)
            {
                result = receiver.ReceiveDamage(request);
            }
            else
            {
                float previous = target.Current;
                target.TakeDamage(amount, isCritical);
                float applied = Mathf.Max(0f, previous - target.Current);
                result = new DamageResult(amount, applied, previous > 0f && target.Current <= 0f);
            }

            NotifyDamageDealt(source, request, result);
            return result;
        }

        public static float Resolve(Component source, float baseAmount)
        {
            float amount = Mathf.Max(0f, baseAmount);
            if (source == null)
                return amount;

            MonoBehaviour[] behaviours = source.GetComponentsInParent<MonoBehaviour>(true);
            List<IOutgoingDamageModifier> modifiers = new List<IOutgoingDamageModifier>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && behaviour is IOutgoingDamageModifier modifier)
                    modifiers.Add(modifier);
            }

            modifiers.Sort(CompareModifiers);
            foreach (IOutgoingDamageModifier modifier in modifiers)
                amount = Mathf.Max(0f, modifier.ModifyOutgoingDamage(amount));

            return amount;
        }

        static int CompareModifiers(IOutgoingDamageModifier left, IOutgoingDamageModifier right)
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0
                ? priority
                : string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal);
        }

        static void NotifyDamageDealt(Component source, DamageRequest request, DamageResult result)
        {
            if (source == null) return;

            MonoBehaviour[] behaviours = source.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && behaviour is IDamageDealtListener listener)
                    listener.OnDamageDealt(request, result);
            }
        }
    }
}
