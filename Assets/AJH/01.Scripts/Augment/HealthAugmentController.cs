using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AugmentDrafter))]
    public class HealthAugmentController : MonoBehaviour, IIncomingDamageModifier, IDamageDealtListener, IOutgoingDamageModifier
    {
        [Header("거인")]
        [SerializeField] float _giantHpBonus = 0.55f;//HP증가 %
        [SerializeField] float _giantScaleBonus = 0.10f;// 크기 증가 %

        [Header("유리대포")]
        [SerializeField] float _glassCannonHpPenalty = 0.30f;//Hp감소 %
        [SerializeField] float _glassCannonDamageBonus = 1.80f;//데미지 증가 %
        [SerializeField] float _glassCannonScalePenalty = 0.10f;//크기 감소 %

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
        [SerializeField] float _deathWaltzSpreadTime = 5f; // 초당 1번씩, 이 초만큼 나눠서 (5초 = 5번)

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
        
        ISpeedable _speedable;

        public int Priority => 0;
        public float ModifyOutgoingDamage(float amount)
        {
            return amount * GlassCannonDamageMultiplier * BerserkerDamageMultiplier;
        }

        public bool BerserkerActive { get; private set; }
        public float BerserkerDamageMultiplier => BerserkerActive ? 1f + _berserkerDamageBonus : 1f;
        public float BerserkerAttackSpeedMultiplier => BerserkerActive ? 1f + _berserkerAtkSpeedBonus : 1f;
        public float GlassCannonDamageMultiplier => Has(CommonAugmentType.GlassCannon) ? _glassCannonDamageBonus : 1f;

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        private DinosaurVisualController _visual;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;
            _speedable = GetComponentInParent<ISpeedable>();
            
            _visual = GetComponentInChildren<DinosaurVisualController>();
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
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            _acquired.Add(common.type);
            Debug.Log($"[증강 획득] {common.type}");

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
            }
        }

        void ApplyMaxHealthMultiplier(float multiplier)
        {
            float oldMax = _health.maxHealth;
            float newMax = oldMax * multiplier;
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

        void HandleHealthChanged(float current, float max)
        {
            bool before = BerserkerActive;
            BerserkerActive = Has(CommonAugmentType.Berserker) && current <= max * _berserkerHpRatio;
            if (before != BerserkerActive)
                Debug.Log($"[광전사] {BerserkerActive} / 체력 {current}/{max}");
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;

            if (Has(CommonAugmentType.Vampire))
                _health.Heal(result.AppliedAmount * _vampireHealRate);

            if (Has(CommonAugmentType.Confidence))
            {
                Debug.Log($"[자신감] 이속 버프, speedable={_speedable}");
                _speedable?.ApplySpeed(_confidenceSpeedBonus, _confidenceDuration);
            }
        }

        public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
        {
            if (_phoenixInvulnActive)
                return 0f;

            if (Has(CommonAugmentType.Phoenix) && !_phoenixUsed && currentAmount >= _health.Current)
            {
                _phoenixUsed = true;
                _phoenixInvulnActive = true;
                _phoenixInvulnTimer = _phoenixInvulnTime;
                _pendingDamage.Clear();         
                _health.Heal(_health.maxHealth);
                //_visual?.PlayRevive();
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