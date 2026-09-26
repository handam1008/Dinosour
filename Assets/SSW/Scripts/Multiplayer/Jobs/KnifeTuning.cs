using System;
using UnityEngine;

namespace SSW
{
    internal static class KnifeTuning
    {
        [Serializable]
        public sealed class Escape
        {
            [SerializeField] float speedMultiplier;
            [SerializeField] float duration;
            [SerializeField] float cooldown;
            public float Speed => speedMultiplier >= 1f ? speedMultiplier - 1f : speedMultiplier;
            public float Duration => duration;
            public float Cooldown => cooldown;
        }

        [Serializable]
        public sealed class Aura
        {
            [SerializeField] float _duration;
            [SerializeField] float _radius;
            [SerializeField] float _maxSpeedDebuffRatio;
            [SerializeField] float _maxDamageDebuffRatio;
            [SerializeField] float _speedRampUpTime;
            [SerializeField] float _damageRampUpTime;
            [SerializeField] float auraCoolTime;
            public float Duration => _duration;
            public float Radius => _radius;
            public float Cooldown => auraCoolTime;
            public float Slow(float elapsed) => Ramp(elapsed, _speedRampUpTime) * _maxSpeedDebuffRatio;
            public float Weaken(float elapsed) => Ramp(elapsed, _damageRampUpTime) * _maxDamageDebuffRatio;
            static float Ramp(float elapsed, float duration) => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        }

        public static T Read<T>(Augment source) => JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
    }
}
