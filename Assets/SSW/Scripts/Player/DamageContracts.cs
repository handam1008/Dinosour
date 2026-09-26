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
        IgnoreDefense = 1 << 5,
        Drain = 1 << 6,
        Deferred = 1 << 7
    }

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

    public readonly struct DamageResult
    {
        public DamageResult(float requestedAmount, float appliedAmount, bool wasLethal, bool wasDeferred = false)
        {
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            WasLethal = wasLethal;
            WasDeferred = wasDeferred;
        }

        public float RequestedAmount { get; }
        public float AppliedAmount { get; }
        public bool WasLethal { get; }
        public bool WasApplied => AppliedAmount > 0f;
        public bool WasDeferred { get; }
        public bool WasAccepted => WasApplied || WasDeferred;
        public bool WasBlocked => RequestedAmount > 0f && !WasAccepted;
    }

    public interface IDamageReceiver
    {
        DamageResult ReceiveDamage(DamageRequest request);
    }

    public interface IDamageDelay
    {
        bool TryDefer(DamageRequest request, float amount);
    }

    public interface IIncomingDamageModifier
    {
        int Priority { get; }
        float ModifyIncomingDamage(DamageRequest request, float currentAmount);
    }

    public interface IDamageDealtListener
    {
        void OnDamageDealt(DamageRequest request, DamageResult result);
    }

    public interface IDamageReceivedListener
    {
        void OnDamageReceived(DamageRequest request, DamageResult result);
    }
}
