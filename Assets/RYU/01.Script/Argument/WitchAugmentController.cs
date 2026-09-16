using System.Collections.Generic;
using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    public class WitchAugmentController : AugmentReceiverBehaviour
    {
        public override PlayerJob Job => PlayerJob.Witch;

        [Header("증강 수치 (밸런싱)")]
        [SerializeField] private float _wideSpray = 1.5f;
        [SerializeField] private float _thickBrew = 1.25f;
        [SerializeField] private float _precisionPower = 0.7f;
        [SerializeField] private int _precisionCount = 3;
        [SerializeField] private int _bounceCount = 1;
        [SerializeField] private float _cycleSpeedUp = 0.6f;

        private readonly HashSet<WitchAugmentType> _has = new HashSet<WitchAugmentType>();
        private readonly List<AbstractPotion> _unlocked = new List<AbstractPotion>();

        public IReadOnlyList<AbstractPotion> Unlocked => _unlocked;

        public override bool TryReceive(Augment augment)
        {
            if (augment is not WitchAugment w) return false;

            _has.Add(w.type);

            if (w.unlockPotions != null)
            {
                foreach (AbstractPotion p in w.unlockPotions)
                {
                    if (p != null && !_unlocked.Contains(p)) _unlocked.Add(p);
                }
            }
            return true;
        }

        public bool Has(WitchAugmentType t) => _has.Contains(t);

        public PotionModifiers BuildModifiers()
        {
            return new PotionModifiers(
                Has(WitchAugmentType.WideSpray) ? _wideSpray : 1f,
                Has(WitchAugmentType.ThickBrew) ? _thickBrew : 1f,
                Has(WitchAugmentType.PrecisionDispensing) ? _precisionPower : 1f,
                Has(WitchAugmentType.RemainingPotion),
                Has(WitchAugmentType.brokenGlass) ? _bounceCount : 0);
        }

        public int ProjectileCount =>
            Has(WitchAugmentType.PrecisionDispensing) ? Mathf.Max(1, _precisionCount) : 1;

        public float CycleInterval(float baseInterval) =>
            Has(WitchAugmentType.AllUnlock) ? baseInterval * _cycleSpeedUp : baseInterval;
    }
}
