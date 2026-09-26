using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class HealthAugmentController : MonoBehaviour, IIncomingDamageModifier, IDamageDealtListener, IOutgoingDamageModifier
    {
        [Header("거인")]
        [SerializeField] float _giantHpBonus = 0.55f;
        [SerializeField] float _giantScaleBonus = 0.10f;

        [Header("유리대포")]
        [SerializeField] float _glassCannonHpPenalty = 0.30f;
        [SerializeField] float _glassCannonDamageBonus = 1.80f;
        [SerializeField] float _glassCannonScalePenalty = 0.10f;

        [Header("불사조")]
        [SerializeField] float _phoenixHpPenalty = 0.25f;
        [SerializeField] float _phoenixInvulnDuration = 2.5f;
        [SerializeField] float _phoenixLockDuration = 1f;
        [SerializeField] float _phoenixScalePenalty = 0.08f;

        [Header("뱀파이어")]
        [SerializeField] float _vampireHealRate = 0.55f;

        [Header("자신감")]
        [SerializeField] float _confidenceSpeedBonus = 0.30f;
        [SerializeField] float _confidenceDuration = 2f;

        [Header("광전사")]
        [SerializeField] float _berserkerHpRatio = 0.75f;
        [SerializeField] float _berserkerSpeedBonus = 0.45f;
        [SerializeField] float _berserkerDamageBonus = 0.60f;

        [Header("죽음의 무도")]
        [SerializeField] float _deathWaltzSpreadTime = 5f;
        [SerializeField] float _deathWaltzHpBonus = 0.3f;

        [Header("문어의 심장")]
        [SerializeField] float _tenLivesHealth = 3f;
        [SerializeField] float _tenLivesDamage = 1f;

        [Header("멀티스케일")]
        [SerializeField] float _multiscaleReduction = 0.50f;

        [Header("티끌모아 태산")]
        [SerializeField] float _regenAmount = 1f;
        [SerializeField] float _regenInterval = 1f;

        readonly HashSet<CommonAugmentType> _acquired = new HashSet<CommonAugmentType>();
        readonly List<(float perTick, int ticksLeft)> _pendingDamage = new();

        AugmentDrafter _drafter;
        Health _health;
        Vector3 _baseScale;
        float _scaleMultiplier = 1f;

        float _deathWaltzTickTimer;
        bool _phoenixUsed;
        bool _phoenixInvulnActive;
        float _phoenixInvulnTimer;
        float _phoenixLockTimer;
        bool _phoenixLocked;
        float _regenTimer;
        float _confidenceUntil;

        DinosaurVisualController _visual;
        PlayerController _player;
        PlayerInput _input;

        public int Priority => 0;

        public float ModifyOutgoingDamage(float amount)
        {
            return amount * GlassCannonDamageMultiplier * BerserkerDamageMultiplier;
        }
        
        public float ConfidenceSpeedMultiplier => Has(CommonAugmentType.Confidence) && Time.time < _confidenceUntil ? 1f + _confidenceSpeedBonus : 1f;

        public bool BerserkerActive { get; private set; }
        public float BerserkerDamageMultiplier => BerserkerActive ? 1f + _berserkerDamageBonus : 1f;
        public float BerserkerSpeedMultiplier => BerserkerActive ? 1f + _berserkerSpeedBonus : 1f;
        
        public float GlassCannonDamageMultiplier => Has(CommonAugmentType.GlassCannon) ? _glassCannonDamageBonus : 1f;

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        bool IsFullHealth => _health.Current >= _health.Max - 0.001f;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;
            _visual = GetComponentInChildren<DinosaurVisualController>(true);
            _player = GetComponentInParent<PlayerController>();
            _input = GetComponentInParent<PlayerInput>();
        }

        void OnEnable()
        {
            _drafter.OnAugmentSelected += HandleSelected;
            _health.OnHealthChanged += HandleHealthChanged;
        }

        void OnDisable()
        {
            _drafter.OnAugmentSelected -= HandleSelected;
            _health.OnHealthChanged -= HandleHealthChanged;
            if (_phoenixLocked) UnlockMovement();
        }
        
        public void MultiplyScale(float multiplier)
        {
            _scaleMultiplier *= multiplier;
            transform.localScale = _baseScale * _scaleMultiplier;
        }

        void Update()
        {
            if (_phoenixInvulnActive)
            {
                _phoenixInvulnTimer -= Time.deltaTime;
                if (_phoenixInvulnTimer <= 0f) _phoenixInvulnActive = false;
            }

            if (_phoenixLocked)
            {
                _phoenixLockTimer -= Time.deltaTime;
                if (_phoenixLockTimer <= 0f) UnlockMovement();
            }

            if (_pendingDamage.Count > 0)
            {
                _deathWaltzTickTimer += Time.deltaTime;
                if (_deathWaltzTickTimer >= 1f)
                {
                    _deathWaltzTickTimer -= 1f;
                    ApplyDeathWaltzTick();
                }
            }

            TickRegeneration();
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            _acquired.Add(common.type);

            switch (common.type)
            {
                case CommonAugmentType.Giant:
                    _scaleMultiplier *= (1f + _giantScaleBonus);
                    transform.localScale = _baseScale * _scaleMultiplier;
                    ApplyMaxHealthMultiplier(1f + _giantHpBonus);
                    break;
                case CommonAugmentType.GlassCannon:
                    _scaleMultiplier *= (1f - _glassCannonScalePenalty);
                    transform.localScale = _baseScale * _scaleMultiplier;
                    ApplyMaxHealthMultiplier(1f - _glassCannonHpPenalty);
                    break;
                case CommonAugmentType.Phoenix:
                    _scaleMultiplier *= (1f - _phoenixScalePenalty);
                    transform.localScale = _baseScale * _scaleMultiplier;
                    ApplyMaxHealthMultiplier(1f - _phoenixHpPenalty);
                    break;
                case CommonAugmentType.DeathWaltz:
                    ApplyMaxHealthMultiplier(1f + _deathWaltzHpBonus);
                    break;
                case CommonAugmentType.TenLives:
                    SetMaxHealth(_tenLivesHealth);
                    break;
            }
        }

        
        public void ApplyMaxHealthMultiplier(float multiplier)
        {
            if (Has(CommonAugmentType.TenLives)) return;
            SetMaxHealth(_health.maxHealth * multiplier);
        }

        void SetMaxHealth(float newMax)
        {
            float oldMax = _health.maxHealth;
            _health.maxHealth = newMax;

            if (newMax > oldMax)
            {
                _health.Heal(newMax - oldMax);
            }
            else
            {
                float excess = _health.Current - newMax;
                if (excess > 0f)
                    _health.ReceiveDamage(new DamageRequest(null, excess, DamageTag.IgnoreDefense));
            }
        }

        void TickRegeneration()
        {
            if (!Has(CommonAugmentType.Regeneration)) return;

            _regenTimer += Time.deltaTime;
            if (_regenTimer < _regenInterval) return;
            _regenTimer -= _regenInterval;

            if (_health.Current > 0f && !IsFullHealth)
                _health.Heal(_regenAmount);
        }

        void HandleHealthChanged(float current, float max)
        {
            BerserkerActive = Has(CommonAugmentType.Berserker) && current <= max * _berserkerHpRatio;
        }
        
        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;

            if (Has(CommonAugmentType.Vampire))
                _health.Heal(result.AppliedAmount * _vampireHealRate);

            if (Has(CommonAugmentType.Confidence))
                _confidenceUntil = Time.time + _confidenceDuration;
        }

        public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
        {
            if (_phoenixInvulnActive)
                return 0f;

            bool external = currentAmount > 0f && !request.HasTag(DamageTag.IgnoreDefense);

            if (external && Has(CommonAugmentType.Multiscale) && IsFullHealth)
                currentAmount *= 1f - _multiscaleReduction;

            if (external && Has(CommonAugmentType.TenLives))
                currentAmount = _tenLivesDamage;

            if (Has(CommonAugmentType.Phoenix) && !_phoenixUsed && currentAmount >= _health.Current)
            {
                _phoenixUsed = true;
                _phoenixInvulnActive = true;
                _phoenixInvulnTimer = _phoenixInvulnDuration;
                _pendingDamage.Clear();
                _health.Heal(_health.maxHealth);
                LockMovement();
                _visual?.PlayRevive();
                return 0f;
            }

            if (request.HasTag(DamageTag.IgnoreDefense))
                return currentAmount;

            if (Has(CommonAugmentType.DeathWaltz) && currentAmount > 0f)
            {
                int ticks = Mathf.Max(1, Mathf.RoundToInt(_deathWaltzSpreadTime));
                _pendingDamage.Add((currentAmount / ticks, ticks));
                return 0f;
            }

            return currentAmount;
        }

        void LockMovement()
        {
            _phoenixLocked = true;
            _phoenixLockTimer = _phoenixLockDuration;
            _input?.DeactivateInput();
            if (_player != null) _player.Simulated = false;
        }

        void UnlockMovement()
        {
            _phoenixLocked = false;
            if (_player != null) _player.Simulated = true;
            if (_input != null && _input.enabled) _input.ActivateInput();
        }

        void ApplyDeathWaltzTick()
        {
            float tickTotal = 0f;
            for (int i = _pendingDamage.Count - 1; i >= 0; i--)
            {
                var (perTick, ticksLeft) = _pendingDamage[i];
                tickTotal += perTick;
                ticksLeft--;

                if (ticksLeft <= 0) _pendingDamage.RemoveAt(i);
                else _pendingDamage[i] = (perTick, ticksLeft);
            }

            if (tickTotal > 0f)
                _health.ReceiveDamage(new DamageRequest(null, tickTotal, DamageTag.IgnoreDefense));
        }

        [ContextMenu("Test: 20 데미지 받기")]
        void Debug_TakeDamage()
        {
            _health.TakeDamage(20f);
        }

        [ContextMenu("Test: 20 회복")]
        void Debug_Heal()
        {
            _health.Heal(20f);
        }
    }
}