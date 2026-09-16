using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    public interface IGamblerCoinHitEffect
    {
        void ApplyCoinHitEffects(Component target, DamageResult damageResult);
    }
}