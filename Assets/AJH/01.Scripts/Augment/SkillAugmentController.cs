using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AugmentDrafter))]
    public class SkillAugmentController : MonoBehaviour,
        IIncomingDamageModifier,
        IOutgoingDamageModifier,
        IDamageDealtListener
    {
        [Header("악마와의 거래")]
        [SerializeField] float _devilsDrainRatio = 0.01f;    // 초당 최대체력의 1% 감소
        [SerializeField] float _devilsStopRatio = 0.10f;     // 현재체력 10% 미만이면 감소 정지
        [SerializeField] float _devilsHealRate = 0.25f;      // 흡혈 25%

        [Header("가만히 있으면 반은 간다")]
        [SerializeField] float _stillRequiredTime = 7f;      // 이만큼 정지하면 발동
        [SerializeField] float _stillBuffDuration = 5f;      // 버프 지속
        [SerializeField] float _stillSpeedBonus = 0.30f;
        [SerializeField] float _stillDamageBonus = 0.50f;
        [SerializeField] float _stillDamageReduction = 0.50f;
        [SerializeField] float _stillMoveThreshold = 0.05f;  // 이 속도 이하를 "정지"로 판단

        [Header("쿨감")]
        [SerializeField] float _cooldownReduction = 0.35f;

        readonly HashSet<CommonAugmentType> _acquired = new HashSet<CommonAugmentType>();

        AugmentDrafter _drafter;
        Health _health;
        Rigidbody2D _body;
        ISpeedable _speedable;

        float _devilsDrainTimer;
        float _stillTimer;
        float _stillBuffEndTime;

        public int Priority => 100;   // HealthAugmentController(0) 다음에 계산되도록

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        public bool StayStillBuffActive => Time.time < _stillBuffEndTime;

        // 직업 스킬 스크립트가 쿨타임에 곱해서 쓰는 값
        public float SkillCooldownMultiplier =>
            Has(CommonAugmentType.CooldownReduction) ? 1f - _cooldownReduction : 1f;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _body = GetComponentInParent<Rigidbody2D>();
            _speedable = GetComponentInParent<ISpeedable>();
        }

        void OnEnable()
        {
            _drafter.OnAugmentSelected += HandleSelected;
        }

        void OnDisable()
        {
            _drafter.OnAugmentSelected -= HandleSelected;
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            _acquired.Add(common.type);
            // 이 계열은 전부 "상황 발생 시" 발동이라 뽑는 순간 할 일이 없음
        }

        void Update()
        {
            TickDevilsDeal();
            TickStayStill();
        }

        // ---------- 악마와의 거래 : 지속 HP 감소 ----------
        void TickDevilsDeal()
        {
            if (!Has(CommonAugmentType.DevilsDeal)) return;

            _devilsDrainTimer += Time.deltaTime;
            if (_devilsDrainTimer < 1f) return;
            _devilsDrainTimer -= 1f;

            // 체력이 일정 비율 밑이면 더 이상 깎지 않음 (이걸로 죽지는 않게)
            if (_health.Current <= _health.Max * _devilsStopRatio) return;

            float amount = _health.Max * _devilsDrainRatio;
            _health.ReceiveDamage(new DamageRequest(this, amount, DamageTag.IgnoreDefense));
        }

        // ---------- 가만히 있으면 반은 간다 ----------
        void TickStayStill()
        {
            if (!Has(CommonAugmentType.StayStill)) return;
            if (StayStillBuffActive) return;   // 버프 중엔 다시 충전하지 않음

            bool moving = _body != null
                && Mathf.Abs(_body.linearVelocity.x) > _stillMoveThreshold;

            if (moving)
            {
                _stillTimer = 0f;
                return;
            }

            _stillTimer += Time.deltaTime;
            if (_stillTimer < _stillRequiredTime) return;

            _stillTimer = 0f;
            _stillBuffEndTime = Time.time + _stillBuffDuration;
            _speedable?.ApplySpeed(_stillSpeedBonus, _stillBuffDuration);
        }

        // ---------- 주는 피해 보정 ----------
        public float ModifyOutgoingDamage(float amount)
        {
            if (Has(CommonAugmentType.StayStill) && StayStillBuffActive)
                amount *= 1f + _stillDamageBonus;

            return amount;
        }

        // ---------- 받는 피해 보정 ----------
        public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
        {
            // 내부적으로 다시 넣는 피해(악마 감소분, 죽음의 무도 틱)는 그대로 통과
            if (request.HasTag(DamageTag.IgnoreDefense))
                return currentAmount;

            // 악마와의 거래 : 환경 피해 면역
            if (Has(CommonAugmentType.DevilsDeal) && request.HasTag(DamageTag.Environment))
                return 0f;

            // 가만히 있으면 반은 간다 : 피해 감소
            if (Has(CommonAugmentType.StayStill) && StayStillBuffActive)
                currentAmount *= 1f - _stillDamageReduction;

            return currentAmount;
        }

        // ---------- 공격 성공 시 ----------
        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;

            if (Has(CommonAugmentType.DevilsDeal))
                _health.Heal(result.AppliedAmount * _devilsHealRate);
        }
    }
}