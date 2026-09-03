using SSW;
using UnityEngine;

namespace NKY.Scripts.Job
{
    [CreateAssetMenu(fileName = "AssassinAugment", menuName = "Player/Augment/Assassin", order = 0)]
    public class AssassinAugment : Augment, IJobRestrictedAugment
    {
        public AssassinAugmentType type;
        public PlayerJob RequiredJob => PlayerJob.Assassin;
    }
}