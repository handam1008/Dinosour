using UnityEngine;

namespace NKY.Scripts.Job
{
    public abstract class AbstractAssassinAugment : MonoBehaviour
    {
        [field: SerializeField] public AssassinAugmentType Type {get; private set;}
        
        public abstract void ApplyAugment();
    }
}