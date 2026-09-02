using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    [CreateAssetMenu(fileName = "NewMagicianAugment", menuName = "RYU/Witch Augment")]
    public class WitchAugment : Augment, IJobRestrictedAugment
    {
        public WitchAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Witch;
    }
}