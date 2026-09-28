using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(fileName = "NewAugment", menuName = "SSW/Augment")]
    public class Augment : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public Augment[] excludes;
    }
}
