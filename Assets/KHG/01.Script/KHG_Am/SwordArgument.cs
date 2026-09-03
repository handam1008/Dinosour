using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewMagicianAugment", menuName = "KHG/Sword Argument")]
    public class SwordArgument : Augment, IJobRestrictedAugment
    {
        public SwordAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Swordsman;
    }
}