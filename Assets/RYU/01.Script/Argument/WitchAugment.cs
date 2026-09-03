using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    [CreateAssetMenu(fileName = "NewWitchAugment", menuName = "RYU/Witch Augment")]
    public class WitchAugment : Augment, IJobRestrictedAugment
    {
        public WitchAugmentType type;

        // 해금형 증강만 채운다. 여기 넣은 포션이 풀에 추가된다.
        public AbstractPotion[] unlockPotions;

        public PlayerJob RequiredJob => PlayerJob.Witch;
    }
}
