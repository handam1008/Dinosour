using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    [DisallowMultipleComponent]
    public class Gambler777AugmentEffects
        : MonoBehaviour, IOutgoingDamageModifier
    {
        [Header("References")]
        [SerializeField] private GamblerAugmentController augmentController;
        [SerializeField] private Jackpot777 jackpot777;

        [Header("Excited")]
        [SerializeField, Min(1f)]
        private float excitedSpeedMultiplier = 1.5f;

        [Header("Overflowing Power")]
        [SerializeField, Min(1f)]
        private float overflowingPowerDamageMultiplier = 1.5f;

        [Header("Jackpot Boost")]
        [SerializeField, Min(1f)]
        private float jackpotBoostDamageMultiplier = 1.3f;

        [SerializeField, Min(1f)]
        private float jackpotBoostSpeedMultiplier = 1.2f;

        private ISpeedable speedable;

        public int Priority => 100;

        private void Awake()
        {
            if (augmentController == null)
            {
                augmentController =
                    GetComponentInParent<GamblerAugmentController>();
            }

            if (jackpot777 == null)
            {
                jackpot777 =
                    GetComponentInParent<Jackpot777>();
            }

            speedable = GetComponentInParent<ISpeedable>();
        }

        private void OnEnable()
        {
            if (jackpot777 != null)
            {
                jackpot777.JackpotStarted += OnJackpotStarted;
            }
        }

        private void OnDisable()
        {
            if (jackpot777 != null)
            {
                jackpot777.JackpotStarted -= OnJackpotStarted;
            }
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (jackpot777 == null
                || !jackpot777.IsActive)
            {
                return amount;
            }

            float multiplier = 1f;

            if (Has(GamblerAugmentType.OverflowingPower))
            {
                multiplier *=
                    overflowingPowerDamageMultiplier;
            }

            if (Has(GamblerAugmentType.JackpotBoost))
            {
                multiplier *=
                    jackpotBoostDamageMultiplier;
            }

            return amount * multiplier;
        }

        private void OnJackpotStarted(float duration)
        {
            if (speedable == null)
            {
                return;
            }

            float speedMultiplier = 1f;

            if (Has(GamblerAugmentType.Excited))
            {
                speedMultiplier *= excitedSpeedMultiplier;
            }

            if (Has(GamblerAugmentType.JackpotBoost))
            {
                speedMultiplier *=
                    jackpotBoostSpeedMultiplier;
            }

            if (speedMultiplier <= 1f)
            {
                return;
            }

            float speedIncreaseAmount =
                speedMultiplier - 1f;

            speedable.ApplySpeed(
                speedIncreaseAmount,
                duration);
        }

        private bool Has(GamblerAugmentType type)
        {
            return augmentController != null
                && augmentController.Has(type);
        }
    }
}
