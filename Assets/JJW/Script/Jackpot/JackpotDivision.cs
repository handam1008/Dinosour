using System;
using JJW.Script.Augments;
using SSW;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JJW.Script.Jackpot
{
    public class JackpotDivision : MonoBehaviour
    {
       [Header("References")]
        [SerializeField] private GamblerCoinShooter coinShooter;
        [SerializeField] private GamblerAugmentController augmentController;
        [SerializeField] private Health health;
        [SerializeField] private GamblerSlotRoulette slotRoulette;

        [Header("Normal Chances")]
        [SerializeField] private float normalDamageChance = 7f;
        [SerializeField] private float normalHealChance = 7f;
        [SerializeField] private float normalInvincibleChance = 7f;
        [SerializeField] private float normalSpeedChance = 7f;
        [SerializeField] private float normalInstantKillChance = 1f;
        [SerializeField] private float normal777Chance = 5f;

        [Header("More Chances Augment")]
        [SerializeField] private float moreDamageChance = 5f;
        [SerializeField] private float moreHealChance = 5f;
        [SerializeField] private float moreInvincibleChance = 5f;
        [SerializeField] private float moreSpeedChance = 5f;
        [SerializeField] private float moreInstantKillChance = 1f;
        [SerializeField] private float more777Chance = 3f;

        [Header("Luck Augment")]
        [SerializeField] private float luckChanceBonus = 0.5f;

        [Header("Probability Shift Augment")]
        [SerializeField] private float probabilityIncrease = 1f;
        [SerializeField] private float maximum777Chance = 10f;

        [Header("Guaranteed Jackpot Augment")]
        [SerializeField] private int requiredWinningCount = 20;

        [Header("Old Coin Augment")]
        [SerializeField] private float oldCoinChance = 1f;

        [Header("Result Values")]
        [SerializeField] private float damageMultiplier = 1.3f;
        [SerializeField] private float healAmount = 50f;
        [SerializeField] private float invincibleDuration = 5f;
        [SerializeField] private float speedIncreaseAmount = 0.523f;
        [SerializeField] private float speedDuration = 7.4f;
        [SerializeField] private float instantKillDamage = 4444f;
        [SerializeField] private float jackpotHealAmount = 30f;
        [SerializeField] private float jackpotDuration = 15f;
        
        private global::Jackpot777 jackpotState;

        private float probabilityShiftBonus;
        private int guaranteedWinningCount;
        private bool jackpotPending;

        public int GuaranteedWinningCount => guaranteedWinningCount;
        public float ProbabilityShiftBonus => probabilityShiftBonus;

        public event Action<float> DamageJackpot;
        public event Action<float> HealJackpot;
        public event Action<float, float> SpeedJackpot;
        public event Action<float, float> Jackpot777;
        public event Action<float> Jackpot444;
        public event Action<float> StarJackpot;
        public event Action<JackpotResultType> ResultDecided;
        
        public event Action<JackpotResultType, JackpotResultType> RouletteCompleted;

        private void Awake()
        {
            if (coinShooter == null)    
            {
                coinShooter =
                    GetComponentInChildren<GamblerCoinShooter>();
            }

            if (augmentController == null)
            {
                augmentController =
                    GetComponentInParent<GamblerAugmentController>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
            if (jackpotState == null)
            {
                jackpotState =
                    GetComponentInParent<global::Jackpot777>();
            }

            if (slotRoulette == null)
            {
                slotRoulette =
                    GetComponentInChildren<GamblerSlotRoulette>(true);
            }
        }

        private void OnEnable()
        {
            if (coinShooter != null)
            {
                coinShooter.RouletteCoinFired += Roulette;
            }

            if (health != null)
            {
                health.OnDied += ResetProbabilityShift;
            }
        }

        private void OnDisable()
        {
            if (coinShooter != null)
            {
                coinShooter.RouletteCoinFired -= Roulette;
            }

            if (health != null)
            {
                health.OnDied -= ResetProbabilityShift;
            }
        }
        private bool IsJackpotActive()
        {
            return jackpotState != null
                   && jackpotState.IsActive;
        }

        private bool IsJackpotUnavailable()
        {
            return IsJackpotActive() || jackpotPending;
        }

        private void Roulette()
        {
            Debug.Log($"룰렛 증강 검사 " +
                      $"컨트롤러 연결: {augmentController != null} / " +
                      $"행운 적용: {HasAugment(GamblerAugmentType.Luck)}",
                this);
            bool usedGuaranteedJackpot = !IsJackpotUnavailable() && HasAugment(GamblerAugmentType.GuaranteedJackpot)
                                                            && guaranteedWinningCount >= requiredWinningCount;

            JackpotResultType mainResult = usedGuaranteedJackpot ? JackpotResultType.Jackpot777 : RollMainResult();

            bool rolledOldCoin = HasAugment(GamblerAugmentType.OldCoin);

            JackpotResultType oldCoinResult = rolledOldCoin ? RollOldCoinResult() : JackpotResultType.None;

            if (usedGuaranteedJackpot)
            {
                guaranteedWinningCount = 0;
            }
            else
            {
                AddGuaranteedJackpotProgress(mainResult, oldCoinResult);
            }

            if (mainResult == JackpotResultType.Jackpot777 || oldCoinResult == JackpotResultType.Jackpot777)
            {
                jackpotPending = true;
                IncreaseProbabilityShift();
            }

            string oldResultText = rolledOldCoin ? oldCoinResult.ToString() : "사용 안 함";

            Debug.Log(
                $"메인 룰렛: {mainResult} / " +
                $"낡은 동전: {oldResultText} / " +
                $"777 확률: {GetCurrent777Chance()}% / " +
                $"확정 스택: {guaranteedWinningCount}" +
                $"/{requiredWinningCount}");

            if (slotRoulette != null)
            {
                slotRoulette.Play(
                    mainResult,
                    () => CompleteRoulette(
                        mainResult,
                        oldCoinResult,
                        rolledOldCoin));
            }
            else
            {
                CompleteRoulette(
                    mainResult,
                    oldCoinResult,
                    rolledOldCoin);
            }
        }

        private void CompleteRoulette(
            JackpotResultType mainResult,
            JackpotResultType oldCoinResult,
            bool rolledOldCoin)
        {
            RouletteCompleted?.Invoke(
                mainResult,
                rolledOldCoin
                    ? oldCoinResult
                    : JackpotResultType.None);

            ApplyResult(mainResult);

            if (ShouldApplyOldCoinResult(mainResult, oldCoinResult))
            {
                ApplyResult(oldCoinResult);
            }

            if (mainResult == JackpotResultType.Jackpot777
                || oldCoinResult == JackpotResultType.Jackpot777)
            {
                jackpotPending = false;
            }
        }

        private JackpotResultType RollMainResult()
        {
            float luckBonus = GetLuckBonus();
            bool hasMoreChances =
                HasAugment(GamblerAugmentType.MoreChances);

            float damageChance = hasMoreChances ? moreDamageChance : normalDamageChance;

            float healChance = hasMoreChances ? moreHealChance : normalHealChance;

            float invincibleChance = hasMoreChances ? moreInvincibleChance : normalInvincibleChance;

            float speedChance = hasMoreChances ? moreSpeedChance : normalSpeedChance;

            float instantKillChance = hasMoreChances ? moreInstantKillChance : normalInstantKillChance;

            float jackpotChance = GetCurrent777Chance();

            return RollTable(damageChance + luckBonus, healChance + luckBonus, invincibleChance + luckBonus, speedChance + luckBonus,
                instantKillChance + luckBonus, jackpotChance);
        }

        private JackpotResultType RollOldCoinResult()
        {
            float luckBonus = GetLuckBonus();
            float chance = oldCoinChance + luckBonus;

            float jackpotChance = IsJackpotUnavailable() ? 0f : chance;

            return RollTable(chance, chance, chance, chance, 0f, jackpotChance);
        }

        private JackpotResultType RollTable(
            float damageChance,
            float healChance,
            float invincibleChance,
            float speedChance,
            float instantKillChance,
            float jackpotChance)
        {
            float roll = Random.Range(0f, 100f);
            float accumulatedChance = 0f;

            if (IsSelected(roll, ref accumulatedChance, damageChance))
            {
                return JackpotResultType.DamageUp;
            }

            if (IsSelected(roll, ref accumulatedChance, healChance))
            {
                return JackpotResultType.Heal;
            }

            if (IsSelected(roll, ref accumulatedChance, invincibleChance))
            {
                return JackpotResultType.Invincible;
            }

            if (IsSelected(roll, ref accumulatedChance, speedChance))
            {
                return JackpotResultType.SpeedUp;
            }

            if (IsSelected(roll, ref accumulatedChance, instantKillChance))
            {
                return JackpotResultType.InstantKill;
            }

            if (IsSelected(roll, ref accumulatedChance, jackpotChance))
            {
                return JackpotResultType.Jackpot777;
            }

            return JackpotResultType.None;
        }

        private bool IsSelected(float roll, ref float accumulatedChance, float chance)
        {
            accumulatedChance += Mathf.Max(0f, chance);
            return roll < accumulatedChance;
        }

        private float GetLuckBonus()
        {
            return HasAugment(GamblerAugmentType.Luck) ? luckChanceBonus : 0f;
        }

        private float GetCurrent777Chance()
        {
            if (IsJackpotUnavailable())
            {
                return 0f;
            }
            bool hasMoreChances = HasAugment(GamblerAugmentType.MoreChances);

            float baseChance = hasMoreChances ? more777Chance : normal777Chance;

            float currentChance = baseChance + GetLuckBonus();

            if (HasAugment(GamblerAugmentType.ProbabilityShift))
            {
                currentChance += probabilityShiftBonus;
                currentChance = Mathf.Min(currentChance, maximum777Chance);
            }

            return currentChance;
        }

        private void IncreaseProbabilityShift()
        {
            if (!HasAugment(GamblerAugmentType.ProbabilityShift))
            {
                return;
            }

            probabilityShiftBonus += probabilityIncrease;
            probabilityShiftBonus = Mathf.Min(probabilityShiftBonus, maximum777Chance);
        }

        private void ResetProbabilityShift()
        {
            probabilityShiftBonus = 0f;
            jackpotPending = false;
        }

        private void AddGuaranteedJackpotProgress(JackpotResultType mainResult, JackpotResultType oldCoinResult)
        {
            if (!HasAugment(GamblerAugmentType.GuaranteedJackpot))
            {
                return;
            }

            if (mainResult != JackpotResultType.None)
            {
                guaranteedWinningCount++;
            }

            if (oldCoinResult != JackpotResultType.None)
            {
                guaranteedWinningCount++;
            }

            guaranteedWinningCount = Mathf.Min(guaranteedWinningCount, requiredWinningCount);
        }

        private bool ShouldApplyOldCoinResult(
            JackpotResultType mainResult, JackpotResultType oldCoinResult)
        {
            if (oldCoinResult == JackpotResultType.None)
            {
                return false;
            }

            if (mainResult != oldCoinResult)
            {
                return true;
            }

            return oldCoinResult == JackpotResultType.DamageUp || oldCoinResult == JackpotResultType.Heal;
        }

        private bool HasAugment(GamblerAugmentType type)
        {
            return augmentController != null && augmentController.Has(type);
        }

        private void ApplyResult(JackpotResultType result)
        {
            switch (result)
            {
                case JackpotResultType.DamageUp: DamageJackpot?.Invoke(damageMultiplier);
                    break;

                case JackpotResultType.Heal: HealJackpot?.Invoke(healAmount);
                    break;

                case JackpotResultType.Invincible: StarJackpot?.Invoke(invincibleDuration);
                    break;

                case JackpotResultType.SpeedUp: SpeedJackpot?.Invoke(speedIncreaseAmount, speedDuration);
                    break;

                case JackpotResultType.InstantKill: Jackpot444?.Invoke(instantKillDamage);
                    break;

                case JackpotResultType.Jackpot777: Jackpot777?.Invoke(jackpotHealAmount, jackpotDuration);
                    break;
            }

            ResultDecided?.Invoke(result);
        }
    }
}
