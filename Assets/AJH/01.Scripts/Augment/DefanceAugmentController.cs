using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AugmentDrafter))]
    [RequireComponent(typeof(Defance))]
    public class DefanceAugmentController : MonoBehaviour,IOutgoingDamageModifier, IDamageDealtListener
    {
        [Header("방어")]
        [SerializeField] float _guardMasteryReduction = 0.30f;

        [Header("무적")]
        [SerializeField] float _invincibleBonusTime = 1f;

        [Header("재충전")]
        [SerializeField] float _rechargeCoolPenalty = 0.40f;
        [SerializeField] float _rechargeGap = 0.1f;

        [Header("힐")]
        [SerializeField] GameObject _healFieldPrefab;

        [Header("점멸")]
        [SerializeField] float _blinkDistance = 4f;
        [SerializeField] float _blinkDuration = 0.15f;

        [Header("아이스 에이지")]
        [SerializeField] float _iceAgeRadius = 5f;
        [SerializeField] float _iceAgeFreezeTime = 0.5f;
        [SerializeField] float _iceAgeSlowAmount = 0.65f;
        [SerializeField] float _iceAgeSlowTime = 2.5f;
        [SerializeField] float _iceAgeCoolPenalty = 0.15f;

        [Header("일격 필살")]
        [SerializeField] float _bestOffenseBonus = 2f;
        [SerializeField] float _bestOffenseWindow = 5f;
        [SerializeField] float _bestOffenseCoolPenalty = 0.50f;
        
        [Header("뉴클리어")]
        [SerializeField] float _nuclearRadius = 10f;
        [SerializeField] float _nuclearDamageRatio = 0.20f;
        [SerializeField] float _nuclearCoolPenalty = 1.50f;
        [SerializeField] LayerMask _nuclearWallMask;   
        [SerializeField] GameObject _nuclearExplosionPrefab;
        

        readonly HashSet<CommonAugmentType> _acquired = new HashSet<CommonAugmentType>();

        AugmentDrafter _drafter;
        Health _health;
        Defance _defance;
        Rigidbody2D _body;
        PlayerController _player;
        ISlowable _selfSlowable;
        Camera _cam;

        float _empowerUntil;
        bool _isRechargeGuard;

        Vector2 _blinkDirection;
        float _blinkTimeLeft;
        bool _isBlinking;

        public int Priority => 200;

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        public bool IsEmpowered => Has(CommonAugmentType.BestOffense) && Time.time < _empowerUntil;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _defance = GetComponent<Defance>();
            _body = GetComponentInParent<Rigidbody2D>();
            _player = GetComponentInParent<PlayerController>();
            _selfSlowable = GetComponentInParent<ISlowable>();
            _cam = Camera.main;
        }

        void OnEnable()
        {
            _drafter.OnAugmentSelected += HandleSelected;
            _defance.OnBarrierUsed += HandleBarrierUsed;
        }

        void OnDisable()
        {
            _drafter.OnAugmentSelected -= HandleSelected;
            _defance.OnBarrierUsed -= HandleBarrierUsed;

            if (_isBlinking) EndBlink();
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            if (!_acquired.Add(common.type)) return;

            switch (common.type)
            {
                case CommonAugmentType.GuardMastery:
                    _defance.CoolDown *= 1f - _guardMasteryReduction;
                    break;
                case CommonAugmentType.Invincible:
                    _defance.BarrierContinue += _invincibleBonusTime;
                    break;
                case CommonAugmentType.Recharge:
                    _defance.CoolDown *= 1f + _rechargeCoolPenalty;
                    break;
                case CommonAugmentType.IceAge:
                    _defance.CoolDown *= 1f + _iceAgeCoolPenalty;
                    break;
                case CommonAugmentType.BestOffense:
                    _defance.CoolDown *= 1f + _bestOffenseCoolPenalty;
                    break;
                case CommonAugmentType.Nuclear:
                    _defance.CoolDown *= 1f + _nuclearCoolPenalty;
                    break;
            }
        }

        void HandleBarrierUsed()
        {
            TriggerGuardEffects();

            if (Has(CommonAugmentType.Recharge) && !_isRechargeGuard)
                StartCoroutine(RechargeRoutine());
        }

        IEnumerator RechargeRoutine()
        {
            yield return new WaitUntil(() => !_defance.IsGuarding);
            yield return new WaitForSeconds(_rechargeGap);

            _isRechargeGuard = true;
            _defance.Guard();
            _isRechargeGuard = false;
        }

        void TriggerGuardEffects()
        {
            Vector2 center = transform.position;

            if (Has(CommonAugmentType.HealField) && _healFieldPrefab != null)
                Instantiate(_healFieldPrefab, center, Quaternion.identity);

            if (Has(CommonAugmentType.IceAge))
                CastIceAge(center);

            if (Has(CommonAugmentType.Nuclear))
                CastNuclear(center);

            if (Has(CommonAugmentType.BestOffense))
                _empowerUntil = Time.time + _bestOffenseWindow;

            if (Has(CommonAugmentType.Blink))
                StartBlink();
        }
        

        void StartBlink()
        {
            if (_body == null) return;

            Vector2 direction = Vector2.zero;
            if (_cam != null && Mouse.current != null)
            {
                Vector2 mouse = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                direction = mouse - _body.position;
            }

            if (direction.sqrMagnitude < 0.0001f)
                direction = new Vector2(_player != null ? _player.FacingSign : 1f, 0f);

            _blinkDirection = direction.normalized;
            _blinkTimeLeft = _blinkDuration;
            _isBlinking = true;
        }

        void FixedUpdate()
        {
            if (!_isBlinking) return;

            if (_blinkTimeLeft <= 0f)
            {
                EndBlink();
                return;
            }

            float step = Mathf.Min(Time.fixedDeltaTime, _blinkTimeLeft);
            float speed = _blinkDistance / _blinkDuration;
            _body.linearVelocity = _blinkDirection * speed * (step / Time.fixedDeltaTime);

            _blinkTimeLeft -= Time.fixedDeltaTime;
        }

        void EndBlink()
        {
            _isBlinking = false;
            _blinkTimeLeft = 0f;

            if (_body == null) return;
            _body.linearVelocity = Vector2.zero;
        }

        void CastIceAge(Vector2 center)
        {
            var targets = new HashSet<ISlowable>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, _iceAgeRadius))
            {
                ISlowable slowable = hit.GetComponentInParent<ISlowable>();
                if (slowable == null || ReferenceEquals(slowable, _selfSlowable)) continue;
                targets.Add(slowable);
            }

            if (targets.Count == 0) return;

            foreach (ISlowable slowable in targets)
            {
                if (slowable is Component component)
                    FreezeEffect.Apply(component.gameObject, _iceAgeFreezeTime);
            }

            StartCoroutine(IceAgeSlowRoutine(targets));
        }

        IEnumerator IceAgeSlowRoutine(HashSet<ISlowable> targets)
        {
            yield return new WaitForSeconds(_iceAgeFreezeTime);

            foreach (ISlowable slowable in targets)
            {
                if (slowable is UnityEngine.Object obj && obj == null) continue;
                slowable.ApplySlow(_iceAgeSlowAmount, _iceAgeSlowTime);
            }
        }

        void CastNuclear(Vector2 center)
        {
            if (_nuclearExplosionPrefab != null)
                Instantiate(_nuclearExplosionPrefab, center, Quaternion.identity);

            var targets = new HashSet<IDamageable>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, _nuclearRadius))
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, _health)) continue;

                // 폭심지에서 대상까지 벽이 가로막고 있으면 피해 없음
                Vector2 point = hit.ClosestPoint(center);
                if (Physics2D.Linecast(center, point, _nuclearWallMask)) continue;

                targets.Add(target);
            }

            foreach (IDamageable target in targets)
                CombatDamage.Deal(null, target, target.Max * _nuclearDamageRatio);
        }

        public float ModifyOutgoingDamage(float amount)
        {
            return IsEmpowered ? amount * (1f + _bestOffenseBonus) : amount;
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!IsEmpowered) return;

            _empowerUntil = 0f;
            if (result.WasApplied)
                _defance.ResetCooldown();
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _nuclearRadius);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _iceAgeRadius);
        }
    }
}