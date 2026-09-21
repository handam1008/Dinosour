using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AugmentDrafter))]
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
        [SerializeField] float _phoenixInvulnTime = 0.5f;
        [SerializeField] float _phoenixScalePenalty = 0.08f;

        [Header("뱀파이어")]
        [SerializeField] float _vampireHealRate = 0.55f;

        [Header("자신감")]
        [SerializeField] float _confidenceSpeedBonus = 0.30f;
        [SerializeField] float _confidenceDuration = 2f;

        [Header("광전사")]
        [SerializeField] float _berserkerHpRatio = 0.75f;
        [SerializeField] float _berserkerAtkSpeedBonus = 0.45f;
        [SerializeField] float _berserkerDamageBonus = 0.60f;

        [Header("죽음의 무도")]
        [SerializeField] float _deathWaltzSpreadTime = 5f;

        [Header("10개의 목숨")]
        [SerializeField] float _tenLivesHealth = 10f;
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
        float _regenTimer;
        float _confidenceUntil;

        ISpeedable _speedable;
        DinosaurVisualController _visual;

        public int Priority => 0;

        public float ModifyOutgoingDamage(float amount)
        {
            return amount * GlassCannonDamageMultiplier * BerserkerDamageMultiplier;
        }
        
        public float ConfidenceSpeedMultiplier => Has(CommonAugmentType.Confidence) && Time.time < _confidenceUntil ? 1f + _confidenceSpeedBonus : 1f;

        public bool BerserkerActive { get; private set; }
        public float BerserkerDamageMultiplier => BerserkerActive ? 1f + _berserkerDamageBonus : 1f;
        public float BerserkerAttackSpeedMultiplier => BerserkerActive ? 1f + _berserkerAtkSpeedBonus : 1f;
        public float GlassCannonDamageMultiplier => Has(CommonAugmentType.GlassCannon) ? _glassCannonDamageBonus : 1f;

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        bool IsFullHealth => _health.Current >= _health.Max - 0.001f;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;
            _speedable = GetComponentInParent<ISpeedable>();
            _visual = GetComponentInChildren<DinosaurVisualController>(true);
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

        // ---------- 뽑는 순간 ----------
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
                case CommonAugmentType.TenLives:
                    SetMaxHealth(_tenLivesHealth);   // 배율이 아니라 고정값
                    break;
            }
        }

        
        public void ApplyMaxHealthMultiplier(float multiplier)
        {
            if (Has(CommonAugmentType.TenLives)) return;   // 10으로 고정된 뒤엔 체력 배율 무시
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

        // ---------- 티끌모아 태산 ----------
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

        // ---------- 내가 때렸을 때 ----------
        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;

            if (Has(CommonAugmentType.Vampire))
                _health.Heal(result.AppliedAmount * _vampireHealRate);

            if (Has(CommonAugmentType.Confidence))
                _speedable?.ApplySpeed(_confidenceSpeedBonus, _confidenceDuration);
        }

        // ---------- 내가 맞을 때 ----------
        public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
        {
            // 1. 불사조 부활 직후 무적
            if (_phoenixInvulnActive)
                return 0f;

            // 밖에서 들어온 진짜 피해인지 (방어로 막힌 0, 내부 틱은 제외)
            bool external = currentAmount > 0f && !request.HasTag(DamageTag.IgnoreDefense);

            // 2. 멀티스케일 — 체력이 가득 찬 상태면 피해 감소
            if (external && Has(CommonAugmentType.Multiscale) && IsFullHealth)
                currentAmount *= 1f - _multiscaleReduction;

            // 3. 10개의 목숨 — 무조건 1 (멀티스케일보다 우선)
            if (external && Has(CommonAugmentType.TenLives))
                currentAmount = _tenLivesDamage;

            // 4. 불사조 — 위에서 줄어든 값 기준으로 죽는지 판단
            if (Has(CommonAugmentType.Phoenix) && !_phoenixUsed && currentAmount >= _health.Current)
            {
                _phoenixUsed = true;
                _phoenixInvulnActive = true;
                _phoenixInvulnTimer = _phoenixInvulnTime;
                _pendingDamage.Clear();
                _health.Heal(_health.maxHealth);
                _visual?.PlayRevive();
                return 0f;
            }

            // 5. 내부 피해는 그대로 통과
            if (request.HasTag(DamageTag.IgnoreDefense))
                return currentAmount;

            // 6. 죽음의 무도
            if (Has(CommonAugmentType.DeathWaltz) && currentAmount > 0f)
            {
                int ticks = Mathf.Max(1, Mathf.RoundToInt(_deathWaltzSpreadTime));
                _pendingDamage.Add((currentAmount / ticks, ticks));
                return 0f;
            }

            return currentAmount;
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