using System;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [Serializable]
    public struct FighterStats : INetworkSerializable, IEquatable<FighterStats>
    {
        public PlayerJob Job;
        public float Health;
        public float MoveSpeed;
        public float JumpSpeed;
        public float Gravity;
        public float Coyote;
        public float Decay;
        public float Brake;
        public float Damage;
        public float AttackInterval;
        public Vector2 HitSize;
        public Vector2 HitOffset;
        public float AttackTime;
        public FlightStats Flight;
        public float SkillCooldown;
        public float SkillDamage;
        public float DashSpeed;
        public float DashTime;
        public float DashCooldown;
        public float DashDamage;
        public float DashRadius;
        public float ParryTime;
        public float ParryCooldown;
        public float ReflectSpeed;
        public int Capacity;
        public float Reload;
        public float ChargeTime;
        public float ChargeDamage;
        public float RollInterval;
        public float MirrorScale;
        public float BrewTime;
        public float ThrowLift;
        public float Spread;
        public float Splash;
        public float GravityDelay;
        public float ExtraGravity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Job);
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref MoveSpeed);
            serializer.SerializeValue(ref JumpSpeed);
            serializer.SerializeValue(ref Gravity);
            serializer.SerializeValue(ref Coyote);
            serializer.SerializeValue(ref Decay);
            serializer.SerializeValue(ref Brake);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref AttackInterval);
            serializer.SerializeValue(ref HitSize);
            serializer.SerializeValue(ref HitOffset);
            serializer.SerializeValue(ref AttackTime);
            serializer.SerializeValue(ref Flight);
            serializer.SerializeValue(ref SkillCooldown);
            serializer.SerializeValue(ref SkillDamage);
            serializer.SerializeValue(ref DashSpeed);
            serializer.SerializeValue(ref DashTime);
            serializer.SerializeValue(ref DashCooldown);
            serializer.SerializeValue(ref DashDamage);
            serializer.SerializeValue(ref DashRadius);
            serializer.SerializeValue(ref ParryTime);
            serializer.SerializeValue(ref ParryCooldown);
            serializer.SerializeValue(ref ReflectSpeed);
            serializer.SerializeValue(ref Capacity);
            serializer.SerializeValue(ref Reload);
            serializer.SerializeValue(ref ChargeTime);
            serializer.SerializeValue(ref ChargeDamage);
            serializer.SerializeValue(ref RollInterval);
            serializer.SerializeValue(ref MirrorScale);
            serializer.SerializeValue(ref BrewTime);
            serializer.SerializeValue(ref ThrowLift);
            serializer.SerializeValue(ref Spread);
            serializer.SerializeValue(ref Splash);
            serializer.SerializeValue(ref GravityDelay);
            serializer.SerializeValue(ref ExtraGravity);
        }

        public bool Equals(FighterStats other) => Job == other.Job && Health.Equals(other.Health)
            && MoveSpeed.Equals(other.MoveSpeed) && JumpSpeed.Equals(other.JumpSpeed) && Gravity.Equals(other.Gravity)
            && Coyote.Equals(other.Coyote) && Decay.Equals(other.Decay) && Brake.Equals(other.Brake)
            && Damage.Equals(other.Damage) && AttackInterval.Equals(other.AttackInterval)
            && HitSize.Equals(other.HitSize) && HitOffset.Equals(other.HitOffset) && AttackTime.Equals(other.AttackTime)
            && Flight.Equals(other.Flight) && SkillCooldown.Equals(other.SkillCooldown) && SkillDamage.Equals(other.SkillDamage)
            && DashSpeed.Equals(other.DashSpeed) && DashTime.Equals(other.DashTime) && DashCooldown.Equals(other.DashCooldown)
            && DashDamage.Equals(other.DashDamage) && DashRadius.Equals(other.DashRadius) && ParryTime.Equals(other.ParryTime)
            && ParryCooldown.Equals(other.ParryCooldown) && ReflectSpeed.Equals(other.ReflectSpeed) && Capacity == other.Capacity
            && Reload.Equals(other.Reload) && ChargeTime.Equals(other.ChargeTime) && ChargeDamage.Equals(other.ChargeDamage)
            && RollInterval.Equals(other.RollInterval) && MirrorScale.Equals(other.MirrorScale) && BrewTime.Equals(other.BrewTime)
            && ThrowLift.Equals(other.ThrowLift) && Spread.Equals(other.Spread) && Splash.Equals(other.Splash)
            && GravityDelay.Equals(other.GravityDelay) && ExtraGravity.Equals(other.ExtraGravity);
    }
}
