using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    public class Giant : MonoBehaviour
    {
        [SerializeField] float _scaleBonus = 0.5f;   // 스택당 +50% 크기
        [SerializeField] float _healthBonus = 0.8f;  // 스택당 +80% 체력
        [SerializeField] int _count;

        AugmentDrafter _drafter;
        Health _health;

        Vector3 _baseScale;
        float _baseMaxHealth;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _drafter.OnAugmentSelected += HandleSelected;

            // 기준값 저장
            _baseScale = transform.localScale;
            _baseMaxHealth = _health.maxHealth;
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

            // 기준값에서부터 다시 계산
            transform.localScale = _baseScale * (1f + _scaleBonus * _count);

            float before = _health.maxHealth;
            _health.maxHealth = _baseMaxHealth * (1f + _healthBonus * _count);
            _health.Heal(_health.maxHealth - before);
        }
    }
}