using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    // 마녀의 흡정: 포션으로 실제 피해를 줬을 때 그 양의 일부를 회복한다.
    // CombatDamage가 공격자(source)의 부모를 훑어 이 컴포넌트를 찾아 불러준다.
    // → 마녀 루트 오브젝트에 붙일 것.
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
            // 막혔거나 이미 죽은 대상이면 회복 없음
            if (!result.WasApplied) return;
            if (_augment == null || !_augment.Has(WitchAugmentType.Lifesteal)) return;

            // 요청 피해가 아니라 '실제로 들어간' 피해 기준
            _self?.Heal(result.AppliedAmount * _healRate);
        }
    }
}
