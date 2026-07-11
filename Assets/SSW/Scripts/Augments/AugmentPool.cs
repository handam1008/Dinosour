using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewAugmentPool", menuName = "SSW/Augment Pool")]
    public class AugmentPool : ScriptableObject
    {
        public Augment[] augments;
    }
}
