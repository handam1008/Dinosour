using UnityEngine;

namespace SSW
{
    public class Giant : MonoBehaviour
    {
        [SerializeField] float _scaleMultiplier = 1.5f;
        [SerializeField] float _healthMultiplier = 1.8f;
        [SerializeField] int _count;

        AugmentDrafter _drafter;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _drafter.OnAugmentSelected += HandleSelected;
        }

        void OnDestroy()
        {
            if (_drafter != null) _drafter.OnAugmentSelected -= HandleSelected;
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            if (common.type != CommonAugmentType.Giant) return;

            _count++;
            transform.localScale *= _scaleMultiplier;

            Health health = GetComponent<Health>();
            float before = health.maxHealth;
            health.maxHealth *= _healthMultiplier;   // 이름도 Add보다 Multiplier가 맞음
            health.Heal(health.maxHealth - before);  // 늘어난 만큼 회복 + 체력바 갱신
        }
    }
}