using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(AugmentDrafter))]
    public class SkillAugmentController : MonoBehaviour, IIncomingDamageModifier, IOutgoingDamageModifier, IDamageDealtListener
    {
        [Header("공용")]
        [SerializeField] float _moveThreshold = 0.05f; 

        [Header("악마와의 거래")]
        [SerializeField] float _devilsDrainRatio = 0.01f;
        [SerializeField] float _devilsStopRatio = 0.10f;
        [SerializeField] float _devilsHealRate = 0.25f;

        [Header("쾌속 접근")]
        [SerializeField] float _swiftSpeedBonus = 0.60f;
        [SerializeField] float _swiftRange = 30f;
        [SerializeField] LayerMask _swiftObstacleMask;
        [SerializeField] LineRenderer _swiftTether;

        [Header("냠냠")]
        [SerializeField] float _nomRadius = 4f;
        [SerializeField] float _nomDamage = 3f;
        [SerializeField] float _nomHealRate = 1f;
        [SerializeField] float _nomInterval = 1f;
        [SerializeField] GameObject _nomAuraObject;

        [Header("자석")]
        [SerializeField] float _magnetRadius = 5f;
        [SerializeField] float _magnetStrength = 2f;
        [SerializeField] float _magnetInterval = 1f;
        [SerializeField] GameObject _magnetAuraObject;

        [Header("축소 엔진")]
        [SerializeField] float _shrinkScale = 0.25f;
        [SerializeField] float _shrinkSpeedBonus = 0.25f;

        [Header("다재다능")]
        [SerializeField] float _versatileHp = 0.10f;
        [SerializeField] float _versatileDamage = 0.10f;
        [SerializeField] float _versatileLifesteal = 0.05f;
        [SerializeField] float _versatileSpeed = 0.10f;
        [SerializeField] float _versatileGuardCool = 0.10f;

        [Header("더블 점프")]
        [SerializeField] float _doubleJumpRatio = 0.9f;

        [Header("쿵!")]
        [SerializeField] float _slamMinHeight = 2f;
        [SerializeField] Vector2 _slamBoxSize = new Vector2(6f, 0.3f);
        [SerializeField] float _slamBoxOffsetY = -0.5f;
        [SerializeField] float _slamBaseDamage = 5f;
        [SerializeField] float _slamDamagePerHeight = 3f;
        [SerializeField] float _slamMaxDamage = 40f;
        [SerializeField] float _slamKnockback = 6f;
        [SerializeField] GameObject _slamEffectPrefab;

        [Header("지뢰밭")]
        [SerializeField] GameObject _minePrefab;
        [SerializeField] float _mineInterval = 3f;

        [Header("느려져라")]
        [SerializeField] float _slowAuraRadius = 4.5f;
        [SerializeField] float _slowAuraAmount = 0.35f;
        [SerializeField] GameObject _slowAuraObject;

        readonly HashSet<CommonAugmentType> _acquired = new HashSet<CommonAugmentType>();
        readonly List<Health> _enemies = new List<Health>();

        AugmentDrafter _drafter;
        Health _health;
        Rigidbody2D _body;
        PlayerController _player;
        ISpeedable _speedable;
        HealthAugmentController _healthAugments;
        Defance _defance;

        float _devilsDrainTimer;
        bool _swiftActive;
        float _nomTimer;
        float _magnetTimer;
        bool _airJumpUsed;
        bool _wasGrounded = true;
        float _fallPeakY;
        float _mineTimer;

        public int Priority => 100;

        public bool Has(CommonAugmentType type) => _acquired.Contains(type);

        Vector2 Velocity => _player != null ? _player.Velocity : Vector2.zero;
        bool IsMoving => Mathf.Abs(Velocity.x) > _moveThreshold;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _health = GetComponent<Health>();
            _body = GetComponentInParent<Rigidbody2D>();
            _player = GetComponentInParent<PlayerController>();
            _speedable = GetComponentInParent<ISpeedable>();
            _healthAugments = GetComponent<HealthAugmentController>();
            _defance = GetComponent<Defance>();
            
            if (_slowAuraObject != null) _slowAuraObject.SetActive(false);
            if (_magnetAuraObject != null) _magnetAuraObject.SetActive(false);
            if (_nomAuraObject != null) _nomAuraObject.SetActive(false);
            if (_swiftTether != null) _swiftTether.enabled = false;
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
            if (!_acquired.Add(common.type)) return;

            switch (common.type)
            {
                case CommonAugmentType.ShrinkEngine:
                    _healthAugments?.MultiplyScale(1f - _shrinkScale);
                    break;
                case CommonAugmentType.Versatile:
                    _healthAugments?.ApplyMaxHealthMultiplier(1f + _versatileHp);
                    if (_defance != null) _defance.CoolDown *= 1f - _versatileGuardCool;
                    break;
                case CommonAugmentType.SlowAura:
                    if (_slowAuraObject != null) _slowAuraObject.SetActive(true);
                    break;
                case CommonAugmentType.Magnet:
                    if (_magnetAuraObject != null) _magnetAuraObject.SetActive(true);
                    break;
                case CommonAugmentType.NomNom:
                    if (_nomAuraObject != null) _nomAuraObject.SetActive(true);
                    break;
            }
        }

        void Update()
        {
            TickDevilsDeal();
            TickSwiftApproach();
            TickNomNom();
            TickMagnet();
            TickLanding();
            TickMinefield();
            TickSlowAura();

            UpdateMoveSpeed();
        }

        void UpdateMoveSpeed()
        {
            float multiplier = 1f;

            if (_swiftActive) multiplier *= 1f + _swiftSpeedBonus;
            if (Has(CommonAugmentType.ShrinkEngine)) multiplier *= 1f + _shrinkSpeedBonus;
            if (Has(CommonAugmentType.Versatile)) multiplier *= 1f + _versatileSpeed;
            if (_healthAugments != null) multiplier *= _healthAugments.ConfidenceSpeedMultiplier;

            if (multiplier > 1.0001f)
                _speedable?.ApplySpeed(multiplier - 1f, 0.1f);
        }

        void TickDevilsDeal()
        {
            if (!Has(CommonAugmentType.DevilsDeal)) return;

            _devilsDrainTimer += Time.deltaTime;
            if (_devilsDrainTimer < 1f) return;
            _devilsDrainTimer -= 1f;

            if (_health.Current <= _health.Max * _devilsStopRatio) return;

            float amount = _health.Max * _devilsDrainRatio;
            _health.ReceiveDamage(new DamageRequest(this, amount, DamageTag.IgnoreDefense));
        }

        void TickSwiftApproach()
        {
            _swiftActive = false;

            if (!Has(CommonAugmentType.SwiftApproach) || _body == null)
            {
                SetTether(false, default, default);
                return;
            }

            Health target = FindNearestEnemy(_swiftRange);
            if (target == null)
            {
                SetTether(false, default, default);
                return;
            }

            Vector2 from = _body.worldCenterOfMass;
            Vector2 to = CenterOf(target);

            if (Physics2D.Linecast(from, to, _swiftObstacleMask))
            {
                SetTether(false, default, default);
                return;
            }

            SetTether(true, from, to);

            float toEnemyX = to.x - from.x;
            _swiftActive = IsMoving && Velocity.x * toEnemyX > 0f;
        }

        void SetTether(bool on, Vector2 a, Vector2 b)
        {
            if (_swiftTether == null) return;

            _swiftTether.enabled = on;
            if (!on) return;

            _swiftTether.positionCount = 2;
            _swiftTether.SetPosition(0, a);
            _swiftTether.SetPosition(1, b);
        }

        void TickNomNom()
        {
            if (!Has(CommonAugmentType.NomNom)) return;

            _nomTimer += Time.deltaTime;
            if (_nomTimer < _nomInterval) return;
            _nomTimer -= _nomInterval;

            foreach (Health enemy in FindEnemies(_nomRadius))
            {
                DamageResult result = CombatDamage.Deal(this, enemy, _nomDamage, DamageTag.JobSkill);
                if (result.WasApplied)
                    _health.Heal(result.AppliedAmount * _nomHealRate);
            }
        }

        void TickMagnet()
        {
            if (!Has(CommonAugmentType.Magnet)) return;

            _magnetTimer += Time.deltaTime;
            if (_magnetTimer < _magnetInterval) return;
            _magnetTimer -= _magnetInterval;

            float myX = transform.position.x;
            foreach (Health enemy in FindEnemies(_magnetRadius))
            {
                IForceReceiver receiver = enemy.GetComponentInParent<IForceReceiver>();
                if (receiver == null) continue;

                float dir = Mathf.Sign(myX - enemy.transform.position.x);
                receiver.ApplyForce(new Vector2(dir * _magnetStrength, 0f), ForceMode2D.Impulse);
            }
        }

        void TickLanding()
        {
            if (_player == null) return;

            bool grounded = _player.IsGrounded;
            float y = transform.position.y;

            if (!grounded)
            {
                _fallPeakY = _wasGrounded ? y : Mathf.Max(_fallPeakY, y);
            }
            else
            {
                _airJumpUsed = false;

                if (!_wasGrounded && Has(CommonAugmentType.Slam))
                {
                    float fallHeight = _fallPeakY - y;
                    if (fallHeight >= _slamMinHeight)
                        DoSlam(fallHeight);
                }
            }

            _wasGrounded = grounded;
        }

        void DoSlam(float height)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(0f, _slamBoxOffsetY);
            float damage = Mathf.Min(_slamBaseDamage + _slamDamagePerHeight * height, _slamMaxDamage);
            float power = _slamKnockback * (1f + height * 0.1f);

            if (_slamEffectPrefab != null)
                Instantiate(_slamEffectPrefab, center, Quaternion.identity);

            foreach (Health enemy in FindEnemiesInBox(center, _slamBoxSize))
            {
                CombatDamage.Deal(this, enemy, damage, DamageTag.JobSkill);

                IForceReceiver receiver = enemy.GetComponentInParent<IForceReceiver>();
                if (receiver == null) continue;

                float dir = Mathf.Sign(enemy.transform.position.x - center.x);
                receiver.ApplyForce(new Vector2(dir, 0.6f) * power, ForceMode2D.Impulse);
            }
        }

        void OnJump(InputValue value)
        {
            if (!value.isPressed) return;
            if (!Has(CommonAugmentType.DoubleJump)) return;
            if (_player == null || _body == null) return;
            if (_player.IsGrounded || _airJumpUsed) return;

            _airJumpUsed = true;
            _body.linearVelocity = new Vector2(
                _body.linearVelocity.x,
                _player.JumpSpeed * _doubleJumpRatio);
        }

        void TickMinefield()
        {
            if (!Has(CommonAugmentType.Minefield) || _minePrefab == null) return;

            _mineTimer += Time.deltaTime;
            if (_mineTimer < _mineInterval) return;
            _mineTimer -= _mineInterval;

            GameObject go = Instantiate(_minePrefab, transform.position, Quaternion.identity);
            go.GetComponent<Mine>()?.Init(this, _health);
        }
        
        void TickSlowAura()
        {
            if (!Has(CommonAugmentType.SlowAura)) return;

            foreach (Health enemy in FindEnemies(_slowAuraRadius))
                enemy.GetComponentInParent<ISlowable>()?.ApplySlow(_slowAuraAmount, 0.15f);
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (Has(CommonAugmentType.Versatile))
                amount *= 1f + _versatileDamage;

            return amount;
        }

        public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
        {
            if (request.HasTag(DamageTag.IgnoreDefense))
                return currentAmount;

            if (Has(CommonAugmentType.DevilsDeal) && request.HasTag(DamageTag.Environment))
                return 0f;

            return currentAmount;
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!result.WasApplied) return;

            float healRate = 0f;
            if (Has(CommonAugmentType.DevilsDeal)) healRate += _devilsHealRate;
            if (Has(CommonAugmentType.Versatile)) healRate += _versatileLifesteal;

            if (healRate > 0f)
                _health.Heal(result.AppliedAmount * healRate);
        }

        List<Health> FindEnemies(float radius)
        {
            return Collect(Physics2D.OverlapCircleAll(transform.position, radius));
        }

        List<Health> FindEnemiesInBox(Vector2 center, Vector2 size)
        {
            return Collect(Physics2D.OverlapBoxAll(center, size, 0f));
        }

        List<Health> Collect(Collider2D[] hits)
        {
            _enemies.Clear();
            foreach (Collider2D hit in hits)
            {
                Health enemy = hit.GetComponentInParent<Health>();
                if (enemy == null || enemy == _health) continue;
                if (!enemy.isActiveAndEnabled || enemy.Current <= 0f) continue;
                if (!_enemies.Contains(enemy)) _enemies.Add(enemy);
            }
            return _enemies;
        }

        Health FindNearestEnemy(float radius)
        {
            Health nearest = null;
            float nearestSqr = float.MaxValue;
            Vector2 me = transform.position;

            foreach (Health enemy in FindEnemies(radius))
            {
                float sqr = ((Vector2)enemy.transform.position - me).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemy;
                }
            }
            return nearest;
        }

        static Vector2 CenterOf(Component target)
        {
            return target.TryGetComponent(out Collider2D col)
                ? (Vector2)col.bounds.center
                : (Vector2)target.transform.position;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 p = transform.position;
            Gizmos.color = Color.cyan;    Gizmos.DrawWireSphere(p, _swiftRange);
            Gizmos.color = Color.red;     Gizmos.DrawWireSphere(p, _nomRadius);
            Gizmos.color = Color.gray; Gizmos.DrawWireSphere(p, _magnetRadius);
            Gizmos.color = Color.blue;    Gizmos.DrawWireSphere(p, _slowAuraRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(p + new Vector3(0f, _slamBoxOffsetY, 0f), _slamBoxSize);
        }
    }
}