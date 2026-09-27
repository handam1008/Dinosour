using System;

namespace SSW
{
    [Serializable]
    public sealed class SwordTuning
    {
        public float ParryReduction = 2f;
        public float BleedDamage = 3f;
        public int BleedTicks = 2;
        public float BleedInterval = 1f;
        public float RootDuration = 0.5f;
        public float RootCooldown = 1f;
        public float Recovery = 0.3f;
        public float DashBonus = 0.15f;
        public float BonusDuration = 4f;
        public float StandingDelay = 1f;
        public float HealInterval = 0.5f;
        public float HealAmount = 1f;
        public float GrowthRate = 0.1f;
        public float GrowthLimit = 3f;
    }
}
