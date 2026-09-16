using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    [CreateAssetMenu(fileName = "NewWitchAugment", menuName = "RYU/Witch Augment")]
    public class WitchAugment : Augment, IJobRestrictedAugment
    {
        public WitchAugmentType type;

        public AbstractPotion[] unlockPotions;

        public PlayerJob RequiredJob => PlayerJob.Witch;
    }
}
