using System.Collections;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    [DisallowMultipleComponent]
    public class GamblerCoinUpgradeEffects
        : MonoBehaviour, IGamblerCoinHitEffect
    {
        [Header("References")]
        [SerializeField] private GamblerAugmentController augmentController;
        [SerializeField] private JackpotDivision division;
        [SerializeField] private DamageUpjackpot damageUpJackpot;

        [Header("Slow")]
        [SerializeField, Range(0f, 1f)] private float slowAmount = 0.2f;
        [SerializeField, Min(0f)] private float slowDuration = 0.5f;

        [Header("Burn")]
        [SerializeField, Min(0f)] private float burnDamagePerTick = 2f;
        [SerializeField, Min(0.01f)] private float burnTickInterval = 0.5f;
        [SerializeField, Min(0f)] private float burnDuration = 2f;

        [Header("Life Steal")]
        [SerializeField, Range(0f, 1f)] private float lifeStealRatio = 0.2f;
        [SerializeField, Min(0f)] private float lifeStealDuration = 10f;

        private IHealable ownerHealable;
        private float slowCoinEndTime;
        private float lifeStealEndTime;
        private bool burnEnabled;

        private void Awake()
        {
            if (augmentController == null)
            {
                augmentController =
                    GetComponentInParent<GamblerAugmentController>();
            }

            if (division == null)
            {
                division = GetComponentInParent<JackpotDivision>();
            }

            if (damageUpJackpot == null)
            {
                damageUpJackpot =
                    GetComponentInParent<DamageUpjackpot>();
            }

            ownerHealable = GetComponentInParent<IHealable>();
        }

        private void OnEnable()
        {
            if (division != null)
            {
                division.SpeedJackpot += OnSpeedJackpot;
                division.HealJackpot += OnHealJackpot;
            }

            if (damageUpJackpot != null)
            {
                damageUpJackpot.DamageStackAdded += OnDamageStackAdded;
                damageUpJackpot.DamageBuffEnded += OnDamageBuffEnded;
            }
        }

        private void OnDisable()
        {
            if (division != null)
            {
                division.SpeedJackpot -= OnSpeedJackpot;
                division.HealJackpot -= OnHealJackpot;
            }

            if (damageUpJackpot != null)
            {
                damageUpJackpot.DamageStackAdded -= OnDamageStackAdded;
                damageUpJackpot.DamageBuffEnded -= OnDamageBuffEnded;
            }

            slowCoinEndTime = 0f;
            lifeStealEndTime = 0f;
            burnEnabled = false;
        }

        public void ApplyCoinHitEffects(
            Component target,
            DamageResult damageResult)
        {
            if (!HasCoinUpgrade())
            {
                return;
            }

            if (target == null || !damageResult.WasApplied)
            {
                return;
            }

            ApplySlow(target);
            ApplyBurn(target);
            ApplyLifeSteal(damageResult.AppliedAmount);
        }

        private void OnSpeedJackpot(
            float amount,
            float duration)
        {
            slowCoinEndTime = Mathf.Max(
                slowCoinEndTime,
                Time.time + duration);
        }

        private void OnHealJackpot(float amount)
        {
            if (!HasCoinUpgrade())
            {
                return;
            }

            lifeStealEndTime = Mathf.Max(
                lifeStealEndTime,
                Time.time + lifeStealDuration);
        }

        private void OnDamageStackAdded(
            bool wasAlreadyActive)
        {
            if (!HasCoinUpgrade())
            {
                return;
            }

            if (wasAlreadyActive)
            {
                burnEnabled = true;
            }
        }

        private void OnDamageBuffEnded()
        {
            burnEnabled = false;
        }

        private void ApplySlow(Component target)
        {
            if (Time.time >= slowCoinEndTime)
            {
                return;
            }

            ISlowable slowable =
                target.GetComponentInParent<ISlowable>();

            if (slowable != null)
            {
                slowable.ApplySlow(
                    slowAmount,
                    slowDuration);
            }
        }

        private void ApplyBurn(Component target)
        {
            if (!burnEnabled)
            {
                return;
            }

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            StartCoroutine(BurnCoroutine(
                target,
                damageable));
        }

        private IEnumerator BurnCoroutine(
            Component target,
            IDamageable damageable)
        {
            float remainingTime = burnDuration;

            while (remainingTime >= burnTickInterval)
            {
                yield return new WaitForSeconds(
                    burnTickInterval);

                remainingTime -= burnTickInterval;

                if (target == null || damageable.Current <= 0f)
                {
                    yield break;
                }

                CombatDamage.Deal(
                    this,
                    damageable,
                    burnDamagePerTick,
                    DamageTag.DamageOverTime
                    | DamageTag.JobSkill);
            }
        }

        private void ApplyLifeSteal(float appliedDamage)
        {
            if (Time.time >= lifeStealEndTime)
            {
                return;
            }

            if (ownerHealable == null)
            {
                return;
            }

            float healAmount = appliedDamage * lifeStealRatio;

            ownerHealable.Heal(healAmount);
        }

        private bool HasCoinUpgrade()
        {
            return augmentController != null && augmentController.Has(GamblerAugmentType.CoinUpgrade);
        }
    }
}