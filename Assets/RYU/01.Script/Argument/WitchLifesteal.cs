using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    public class WitchLifesteal : MonoBehaviour, IDamageDealtListener
    {
        [SerializeField] private float _healRate = 0.15f;

        private WitchAugmentController _augment;
        private IHealable _self;

        private void Awake()
        {
            _augment = GetComponentInParent<WitchAugmentController>();
            _self = GetComponentInParent<IHealable>();
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;
            if (_augment == null || !_augment.Has(WitchAugmentType.Lifesteal)) return;

            _self?.Heal(result.AppliedAmount * _healRate);
        }
    }
}
