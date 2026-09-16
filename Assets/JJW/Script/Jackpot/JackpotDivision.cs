using System;
using JJW.Script.Augments;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JJW.Script.Jackpot
{
    public class JackpotDivision : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GamblerCoinShooter coinShooter;
        [SerializeField] private GamblerAugmentController augmentController;

        [Header("Base Chances")]
        [SerializeField, Range(0f, 100f)] private float damageChance = 7f;
        [SerializeField, Range(0f, 100f)] private float healChance = 7f;
        [SerializeField, Range(0f, 100f)] private float invincibleChance = 7f;
        [SerializeField, Range(0f, 100f)] private float speedChance = 7f;
        [SerializeField, Range(0f, 100f)] private float instantKillChance = 1f;
        [SerializeField, Range(0f, 100f)] private float jackpot777Chance = 5f;

        [Header("Luck Augment")]
        [SerializeField, Min(0f)] private float luckChanceBonus = 0.5f;

        [Header("Result Values")]
        [SerializeField] private float damageMultiplier = 1.3f;
        [SerializeField] private float healAmount = 50f;
        [SerializeField] private float invincibleDuration = 5f;
        [SerializeField] private float speedIncreaseAmount = 0.523f;
        [SerializeField] private float speedDuration = 7.4f;
        [SerializeField] private float instantKillDamage = 4444f;
        [SerializeField] private float jackpotHealAmount = 30f;
        [SerializeField] private float jackpotDuration = 15f;

        public event Action<float> DamageJackpot;
        public event Action<float> HealJackpot;
        public event Action<float, float> SpeedJackpot;
        public event Action<float, float> Jackpot777;
        public event Action<float> Jackpot444;
        public event Action<float> StarJackpot;

        public event Action<JackpotResultType> ResultDecided;

        private void Awake()
        {
            if (coinShooter == null)
            {
                coinShooter = GetComponentInChildren<GamblerCoinShooter>();
            }

            if (augmentController == null)
            {
                augmentController =
                    GetComponentInParent<GamblerAugmentController>();
            }
        }

        private void OnEnable()
        {
            if (coinShooter != null)
            {
                coinShooter.RouletteCoinFired += Roulette;
            }
        }

        private void OnDisable()
        {
            if (coinShooter != null)
            {
                coinShooter.RouletteCoinFired -= Roulette;
            }
        }

        private void Roulette()
        {
            float chanceBonus = GetLuckChanceBonus();
            JackpotResultType result = RollResult(chanceBonus);

            Debug.Log(
                $"룰렛 결과: {result} / 행운 보너스: {chanceBonus}%");

            InvokeResult(result);
            ResultDecided?.Invoke(result);
        }

        private float GetLuckChanceBonus()
        {
            if (augmentController == null)
            {
                return 0f;
            }

            if (!augmentController.Has(GamblerAugmentType.Luck))
            {
                return 0f;
            }

            return luckChanceBonus;
        }

        private JackpotResultType RollResult(float bonusChance)
        {
            float roll = Random.Range(0f, 100f);
            float accumulatedChance = 0f;

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    damageChance + bonusChance))
            {
                return JackpotResultType.DamageUp;
            }

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    healChance + bonusChance))
            {
                return JackpotResultType.Heal;
            }

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    invincibleChance + bonusChance))
            {
                return JackpotResultType.Invincible;
            }

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    speedChance + bonusChance))
            {
                return JackpotResultType.SpeedUp;
            }

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    instantKillChance + bonusChance))
            {
                return JackpotResultType.InstantKill;
            }

            if (IsSelected(
                    roll,
                    ref accumulatedChance,
                    jackpot777Chance + bonusChance))
            {
                return JackpotResultType.Jackpot777;
            }

            return JackpotResultType.None;
        }

        private bool IsSelected(
            float roll,
            ref float accumulatedChance,
            float chance)
        {
            accumulatedChance += Mathf.Max(0f, chance);
            return roll < accumulatedChance;
        }

        private void InvokeResult(JackpotResultType result)
        {
            switch (result)
            {
                case JackpotResultType.DamageUp:
                    DamageJackpot?.Invoke(damageMultiplier);
                    break;

                case JackpotResultType.Heal:
                    HealJackpot?.Invoke(healAmount);
                    break;

                case JackpotResultType.Invincible:
                    StarJackpot?.Invoke(invincibleDuration);
                    break;

                case JackpotResultType.SpeedUp:
                    SpeedJackpot?.Invoke(
                        speedIncreaseAmount,
                        speedDuration);
                    break;

                case JackpotResultType.InstantKill:
                    Jackpot444?.Invoke(instantKillDamage);
                    break;

                case JackpotResultType.Jackpot777:
                    Jackpot777?.Invoke(
                        jackpotHealAmount,
                        jackpotDuration);
                    break;
            }
        }
    }
}