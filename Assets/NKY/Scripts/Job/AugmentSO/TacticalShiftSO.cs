using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "TacticalShift", menuName = "Player/job/Augment/Assassin/TacticalShift", order = 0)]
    public class TacticalShiftSO : AbstractAssassinAugmentSO
    {
        [Header("조건 및 범위")]
        [SerializeField] private float _healthThresholdRatio = 0.3f; // 체력 30% 이하
        [SerializeField] private float _aoeRadius = 4.0f;             // 4m 범위
        [SerializeField] private LayerMask _enemyLayer;
        
        [Header("시각 이펙트")]
        [SerializeField] private GameObject _rangeVfxPrefab; // 원형 범위 표시 프리팹
        [SerializeField] private float _vfxDisplayTime = 1.0f; // 이펙트 유지 시간
        
        [Header("적 디버프")]
        [SerializeField] private float _enemySlowRatio = 0.5f;
        [SerializeField] private float _enemySlowDuration = 7.0f;

        [Header("자신 버프/디버프")]
        [SerializeField] private float _selfSlowRatio = 0.7f;
        [SerializeField] private float _damageMultiplier = 1.5f;
        [SerializeField] private float _selfBuffDuration = 10.0f;
        
        public override void OnHealthChanged(AugmentContext ctx, float currentHp, float maxHp)
        {
            // 라운드 당 1회 제한 체크 (Flag가 true면 이미 발동됨)
            if (ctx.GetFlag(type)) return;
            
            if (currentHp / maxHp <= _healthThresholdRatio)
            {
                // 이번 라운드 발동 플래그 차단
                ctx.SetFlag(type, true);
                
                SpawnRangeVfx(ctx);
                ApplyEnemyDebuff(ctx);
                ctx.Runner.StartCoroutine(Co_ApplySelfBuff(ctx));
            }
        }
        
        private void SpawnRangeVfx(AugmentContext ctx)
        {
            if (_rangeVfxPrefab == null) return;

            // 시전자 위치에 범위 이펙트 생성
            GameObject vfx = Instantiate(_rangeVfxPrefab, ctx.Owner.transform.position, Quaternion.identity);

            // 4m 반지름에 맞게 스케일 조정 (기본 원 지름 = 반지름 * 2)
            vfx.transform.localScale = Vector3.one * (_aoeRadius * 2f);

            // 일정 시간 후 자동 삭제
            Destroy(vfx, _vfxDisplayTime);
        }
        
        public override float ModifyDamage(AugmentContext ctx, DamageInfo info)
        {
            if (ctx.GetFlag(AssassinAugmentType.OnTacticalShift))
            {
                return info.Damage * _damageMultiplier;
            }
            return info.Damage;
        }

        // 3. 라운드 시작 시 호출되는 훅 (발동 기회 초기화)
        public override void OnRoundStart(AugmentContext ctx)
        {
            Debug.Log(ctx.GetFlag(type));
            ctx.SetFlag(type, false);
            ctx.SetFlag(AssassinAugmentType.OnTacticalShift, false);
        }
        
        private void ApplyEnemyDebuff(AugmentContext ctx)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(ctx.Owner.transform.position, _aoeRadius, _enemyLayer);
            foreach (var enemy in enemies)
            {
                if (enemy.TryGetComponent<ISlowable>(out var speedComponent))
                {
                    speedComponent.ApplySlow(_enemySlowRatio, _enemySlowDuration);
                }
            }
        }

        private IEnumerator Co_ApplySelfBuff(AugmentContext ctx)
        {
            // 플레이어 이속 감소 및 데미지 증가 플래그 켜기
            if (ctx.Owner.TryGetComponent<ISlowable>(out var playerSpeed))
            {
                playerSpeed.ApplySlow(_selfSlowRatio, _selfBuffDuration);
            }

            ctx.SetFlag(AssassinAugmentType.OnTacticalShift, true);

            yield return new WaitForSeconds(_selfBuffDuration);

            // 지속시간 종료 후 데미지 증가 해제
            ctx.SetFlag(AssassinAugmentType.OnTacticalShift, false);
        }
    }
}