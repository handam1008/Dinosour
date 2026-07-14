using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewCommonAugment", menuName = "SSW/Common Augment")]
    public class CommonAugment : Augment
    {
        public CommonAugmentType type;
        public CommonAugmentCategory category;
    }
}
