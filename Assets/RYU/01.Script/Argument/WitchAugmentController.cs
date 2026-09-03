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
        [SerializeField] private float _wideSpray = 1.5f;        // 넓은 살포: 반경 배율
        [SerializeField] private float _thickBrew = 1.25f;       // 진한 농도: 틱 횟수 배율
        [SerializeField] private float _precisionPower = 0.7f;   // 정밀 조제: 개당 위력
        [SerializeField] private int _precisionCount = 3;        // 정밀 조제: 던지는 개수
        [SerializeField] private int _bounceCount = 1;           // 깨진 유리병: 추가 폭발 횟수
        [SerializeField] private float _cycleSpeedUp = 0.6f;     // 여러 해금: 교체 주기 배율

        private readonly HashSet<WitchAugmentType> _has = new HashSet<WitchAugmentType>();
        private readonly List<AbstractPotion> _unlocked = new List<AbstractPotion>();

        // 해금 증강으로 풀에 추가된 포션들
        public IReadOnlyList<AbstractPotion> Unlocked => _unlocked;

        public override bool TryReceive(Augment augment)
        {
            if (augment is not WitchAugment w) return false;

            _has.Add(w.type);

            // 해금형 증강이면 포션을 풀에 추가한다
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

        // 포션이 터질 때 쓰는 배율 묶음
        public PotionModifiers BuildModifiers()
        {
            return new PotionModifiers(
                Has(WitchAugmentType.WideSpray) ? _wideSpray : 1f,
                Has(WitchAugmentType.ThickBrew) ? _thickBrew : 1f,
                Has(WitchAugmentType.PrecisionDispensing) ? _precisionPower : 1f,
                Has(WitchAugmentType.RemainingPotion),
                Has(WitchAugmentType.brokenGlass) ? _bounceCount : 0);
        }

        // 정밀 조제: 한 번에 던지는 개수
        public int ProjectileCount =>
            Has(WitchAugmentType.PrecisionDispensing) ? Mathf.Max(1, _precisionCount) : 1;

        // 여러 포션 해금: 교체 주기가 짧아진다
        public float CycleInterval(float baseInterval) =>
            Has(WitchAugmentType.AllUnlock) ? baseInterval * _cycleSpeedUp : baseInterval;
    }
}
