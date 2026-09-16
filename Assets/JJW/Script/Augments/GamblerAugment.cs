using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    [CreateAssetMenu(fileName = "NewGamblerAugment", menuName = "JJW/Gambler Augment")]
    public class GamblerAugment : Augment, IJobRestrictedAugment
    {
        public GamblerAugmentType type;

        public PlayerJob RequiredJob => PlayerJob.Gambler;
    }
}


