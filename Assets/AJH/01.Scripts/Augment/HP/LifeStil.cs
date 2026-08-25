using SSW;
using UnityEngine;
using System.Collections.Generic;

public class LifeStil : MonoBehaviour, IDamageDealtListener
{
    AugmentDrafter _drafter;
    IHealable _owner;
    int _count;

    readonly Dictionary<int, int> _healRatePercent = new()
    {
        { 1, 15 }, // 1스택: 입힌 피해의 15% 흡혈
        { 2, 30 },
        { 3, 50 },
    };

    void Awake()
    {
        _owner = GetComponentInParent<IHealable>();
        if (_drafter == null) _drafter = GetComponent<AugmentDrafter>();
    }

    void OnEnable()
    {
        if (_drafter != null) _drafter.OnAugmentSelected += HandleSelected;
    }

    void OnDisable()
    {
        if (_drafter != null) _drafter.OnAugmentSelected -= HandleSelected;
    }

    void HandleSelected(Augment augment)
    {
        if (augment is not CommonAugment common) return;
        if (common.type != CommonAugmentType.Vampire) return;
        _count++;
    }

    public void OnDamageDealt(DamageRequest request, DamageResult result)
    {
        if (_count == 0) return;
        if (!result.WasApplied) return;

        int level = Mathf.Min(_count, _healRatePercent.Count);
        int rate = _healRatePercent[level];

        _owner?.Heal(result.AppliedAmount * (rate / 100f));
    }
}