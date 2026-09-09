using System.Collections;
using NKY.Lib.EventChannel.EventChannelAsset;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKY.Scripts
{
    public abstract class AbstractWeapon : MonoBehaviour, IAttackBuffable
    {
        [SerializeField] protected LayerMask targetMask;
        [SerializeField] protected float damage;
        [SerializeField] protected float damageMultiplier = 1f;
        [SerializeField] protected float attackCooldown;
        [SerializeField] protected DamageInfoEventChannelSO damageCalcEvent;
        [SerializeField] protected GameObjectEventChannelSO _onBasicAttackChannel;

        // 현재 적용 중인 공격력 디버프 비율 (0.3 = 30% 감소)
        private float _currentDebuffRatio = 0f;
        private Coroutine _debuffCoroutine;

        // 최종 공격력 배율 (기본 배율 * (1 - 디버프 비율))
        public float CurrentDamageMultiplier => Mathf.Max(0f, damageMultiplier * (1f - _currentDebuffRatio));

        protected float _currentCooldown = -99f;
        protected Animator _attackAnim;
        protected Animator _effectAnim;
        protected float _currentAngle;
        protected bool isAttacking = false;
        protected Vector2 currentDirection;
        protected Camera _cam;

        protected virtual void Awake()
        {
            _cam = Camera.main;
        }

        protected virtual void Update()
        {
            FaceAttack();
        }

        public virtual void FaceAttack()
        {
            Vector3 mouse = Mouse.current.position.ReadValue();
            mouse.z = -_cam.transform.position.z;

            Vector3 world = _cam.ScreenToWorldPoint(mouse);
            world.z = transform.position.z;

            Vector3 diff = world - transform.position;
            currentDirection = diff.normalized;
            _currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        }
        
        public virtual void Attack() { }

        // 디버프 적용/갱신 함수
        public void ApplyAttackDebuff(float debuffRatio, float duration)
        {
            // 더 높은 디버프 비율로 갱신
            _currentDebuffRatio = debuffRatio;

            // 이미 타이머가 돌아가는 중이면 리셋 (아우라 틱 지원)
            if (_debuffCoroutine != null)
            {
                StopCoroutine(_debuffCoroutine);
            }

            _debuffCoroutine = StartCoroutine(Co_AttackDebuffTimer(duration));
        }

        private IEnumerator Co_AttackDebuffTimer(float duration)
        {
            yield return new WaitForSeconds(duration);

            // 지속시간이 끝나면 디버프 해제
            _currentDebuffRatio = 0f;
            _debuffCoroutine = null;
        }

        protected virtual void CalcDamage(IDamageable damageable, GameObject target)
        {
            DamageInfo info = new DamageInfo
            {
                Damage = damage * CurrentDamageMultiplier,
                Target = target,
                IsBasicAttack = true
            };

            _onBasicAttackChannel?.Raise(target);
            damageCalcEvent?.Raise(info);
            damageable.TakeDamage(info.Damage);
        }
    }
}