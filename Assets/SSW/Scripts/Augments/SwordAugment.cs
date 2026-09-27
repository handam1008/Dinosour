using UnityEngine;

namespace SSW
{
    public enum SwordPerk { ParryHeal, DashSpeed, DashRange, DashPower, ParryCooldown, DashBleed, DashRoot, DashRecovery, StandingHeal, SwordGrowth }

    [CreateAssetMenu(menuName = "Augment/Sword", fileName = "SwordAugment")]
    public sealed class SwordAugment : Augment, IJobRestrictedAugment
    {
        public SwordPerk type;
        public PlayerJob RequiredJob => PlayerJob.Swordsman;
    }
}
