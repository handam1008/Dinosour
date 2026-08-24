using System;
using UnityEngine;

namespace SSW
{
    [Flags]
    public enum DamageTag
    {
        None = 0,
        BasicAttack = 1 << 0,
        JobSkill = 1 << 1,
        Projectile = 1 << 2,
        DamageOverTime = 1 << 3,
        Environment = 1 << 4,
        IgnoreDefense = 1 << 5
    }

    /// <summary>Everything a receiver needs to decide how damage is handled.</summary>
    public readonly struct DamageRequest
    {
        public DamageRequest(
            Component source,
            float amount,
            DamageTag tags = DamageTag.None,
            bool isCritical = false)
        {
            Source = source;
            Amount = amount;
            Tags = tags;
            IsCritical = isCritical;
        }

        public Component Source { get; }
        public float Amount { get; }
        public DamageTag Tags { get; }
        public bool IsCritical { get; }

        public bool HasTag(DamageTag tag)
        {
            return (Tags & tag) != 0;
        }
    }

    /// <summary>Actual outcome after defense and health clamping.</summary>
    public readonly struct DamageResult
    {
        public DamageResult(float requestedAmount, float appliedAmount, bool wasLethal)
        {
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            WasLethal = wasLethal;
        }

        public float RequestedAmount { get; }
        public float AppliedAmount { get; }
        public bool WasLethal { get; }
        public bool WasApplied => AppliedAmount > 0f;
        public bool WasBlocked => RequestedAmount > 0f && !WasApplied;
    }

    public interface IDamageReceiver
    {
        DamageResult ReceiveDamage(DamageRequest request);
    }

    /// <summary>Defense, invulnerability, delayed damage, and similar receive-side rules.</summary>
    public interface IIncomingDamageModifier
    {
        int Priority { get; }
        float ModifyIncomingDamage(DamageRequest request, float currentAmount);
    }

    /// <summary>Attach to an attacker to react to the resolved result (lifesteal, quests, marks).</summary>
    public interface IDamageDealtListener
    {
        void OnDamageDealt(DamageRequest request, DamageResult result);
    }

    /// <summary>Attach to a target to react after damage is resolved (escape speed, retaliation).</summary>
    public interface IDamageReceivedListener
    {
        void OnDamageReceived(DamageRequest request, DamageResult result);
    }
}
