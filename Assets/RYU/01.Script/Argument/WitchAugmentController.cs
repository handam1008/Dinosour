using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Argument
{
    public class WitchAugmentController : AugmentReceiverBehaviour
    {
        public override PlayerJob Job => PlayerJob.Witch;
        
        readonly HashSet<WitchAugmentType> _has = new();
        
        public override bool TryReceive(Augment augment)
        {
            if (augment is not WitchAugment w) return false;
            _has.Add(w.type);
            return true;
        }
        
        public bool Has(WitchAugmentType t) => _has.Contains(t);

        public float SplashMultiplier => Has(WitchAugmentType.WideSpray) ? 1.3f : 1f;
    }
}