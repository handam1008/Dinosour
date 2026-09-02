using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewMagicianAugment", menuName = "KHG/Sword Argument")]
    public class GunnerArgument : Augment, IJobRestrictedAugment
    {
        public SwordAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Swordsman;
    }
}