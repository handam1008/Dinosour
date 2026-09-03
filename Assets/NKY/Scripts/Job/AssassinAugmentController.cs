using System;
using System.Collections.Generic;
using System.Linq;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Job
{
    public class AssassinAugmentController : AugmentReceiverBehaviour
    {
        readonly HashSet<AssassinAugmentType> _has = new();
        public override PlayerJob Job => PlayerJob.Assassin;

        [SerializeField] private List<AbstractAssassinAugment> assassinAugmentList = new();

        private void Start()
        {
            assassinAugmentList = GetComponentsInChildren<AbstractAssassinAugment>().ToList();
        }

        public override bool TryReceive(Augment augment)
        {
            if (augment is not AssassinAugment w) return false;
            _has.Add(w.type);
            return true;
        }
        
        public bool Has(AssassinAugmentType t) => _has.Contains(t);

        // 게임 코드가 읽어갈 값. 없으면 1배(= 효과 없음)
        public float SplashMultiplier => Has(AssassinAugmentType.ConcealedWeapon) ? 1.15f : 1f;
    }
}