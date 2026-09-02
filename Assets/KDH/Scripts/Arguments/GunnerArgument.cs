using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewMagicianAugment", menuName = "KDH/Gunner Argument")]
    public class GunnerArgument : Augment, IJobRestrictedAugment
    {
        public GunnerAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Gunner;
    }
}