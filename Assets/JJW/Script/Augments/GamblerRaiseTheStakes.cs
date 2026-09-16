using System;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    [DisallowMultipleComponent]
    public class GamblerRaiseTheStakes
        : MonoBehaviour, IOutgoingDamageModifier
    {
        [Header("References")]
        [SerializeField] private GamblerAugmentController augmentController;
        [SerializeField] private JackpotDivision division;
        [SerializeField] private Health health;

        [Header("Stack Settings")]
        [SerializeField, Min(1)] private int maximumStacks = 3;
        [SerializeField, Range(0f, 1f)]
        private float damageIncreasePerStack = 0.1f;

        private int currentStacks;

        public int CurrentStacks => currentStacks;
        public int MaximumStacks => maximumStacks;

        public int Priority => 50;

        public event Action<int, int> StackChanged;

        private void Awake()
        {
            if (augmentController == null)
            {
                augmentController =
                    GetComponentInParent<GamblerAugmentController>();
            }

            if (division == null)
            {
                division =
                    GetComponentInParent<JackpotDivision>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
        }

        private void OnEnable()
        {
            if (division != null)
            {
                division.RouletteCompleted += OnRouletteCompleted;
            }

            if (health != null)
            {
                health.OnDied += ResetStacks;
            }
        }

        private void OnDisable()
        {
            if (division != null)
            {
                division.RouletteCompleted -= OnRouletteCompleted;
            }

            if (health != null)
            {
                health.OnDied -= ResetStacks;
            }
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (!HasAugment() || currentStacks <= 0)
            {
                return amount;
            }

            float damageMultiplier =
                1f + damageIncreasePerStack * currentStacks;

            return amount * damageMultiplier;
        }

        private void OnRouletteCompleted(
            JackpotResultType mainResult,
            JackpotResultType oldCoinResult)
        {
            if (!HasAugment())
            {
                return;
            }

            bool mainWon =
                mainResult != JackpotResultType.None;

            bool oldCoinWon =
                oldCoinResult != JackpotResultType.None;

            if (mainWon || oldCoinWon)
            {
                SetStacks(currentStacks + 1);
            }
            else
            {
                ResetStacks();
            }
        }

        private void SetStacks(int amount)
        {
            int newStacks = Mathf.Clamp(
                amount,
                0,
                maximumStacks);

            if (currentStacks == newStacks)
            {
                return;
            }

            currentStacks = newStacks;

            StackChanged?.Invoke(
                currentStacks,
                maximumStacks);

            Debug.Log(
                $"판돈 올리기: {currentStacks}/{maximumStacks} 스택");
        }

        private void ResetStacks()
        {
            SetStacks(0);
        }

        private bool HasAugment()
        {
            return augmentController != null
                && augmentController.Has(
                    GamblerAugmentType.RaiseTheStakes);
        }
    }
}