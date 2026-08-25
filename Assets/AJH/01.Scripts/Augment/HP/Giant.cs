using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    public class Giant : MonoBehaviour
    {
        private Dictionary<int, (int HpUp, int ScaleUp)> _giant = new()
        {
            { 1, (15, 30) },
            { 2, (30, 60) },
            { 3, (50, 100) }
        };

        AugmentDrafter _drafter;
        Health _health;
        [SerializeField]private int _count = 0;

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
            ApplyGiant();
            //_count++;
            //int level = Mathf.Min(_count, _giant.Count);

            //var (hpUp, scaleUp) = _giant[level];

            //transform.localScale = _baseScale * (1f + scaleUp / 100f);

            //float before = _health.maxHealth;
            //_health.maxHealth = _baseMaxHealth * (1f + hpUp / 100f);
            //_health.Heal(_health.maxHealth - before);
        }

        #region  Test

        [ContextMenu("Test")]
        void ApplyGiant()
        {
            _count++;
            int level = Mathf.Min(_count, _giant.Count);
            var (hpUp, scaleUp) = _giant[level];

            transform.localScale = _baseScale * (1f + scaleUp / 100f);

            float before = _health.maxHealth;
            _health.maxHealth = _baseMaxHealth * (1f + hpUp / 100f);
            _health.Heal(_health.maxHealth - before);
        }

        #endregion
    }
}