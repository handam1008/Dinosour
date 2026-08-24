using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewMagicianAugment", menuName = "SSW/Magician Augment")]
    public class MagicianAugment : Augment, IJobRestrictedAugment
    {
        public MagicianAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Magician;
    }
}
