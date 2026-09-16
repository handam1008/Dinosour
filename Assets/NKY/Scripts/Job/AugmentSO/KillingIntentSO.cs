using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "KillingIntent", menuName = "Player/job/Augment/Assassin/KillingIntent")]
    public class KillingIntentSO : AbstractAssassinAugmentSO
    {
        [Header("아우라 설정")]
        [SerializeField] private float _duration = 6.0f;        // 아우라 지속시간
        [SerializeField] private float _radius = 3.0f;          // 반경 3m
        [SerializeField] private float _maxSpeedDebuffRatio = 0.3f;  // 최대 30% 감소
        [SerializeField] private float _maxDamageDebuffRatio = 0.3f;
        [SerializeField] private float _speedRampUpTime = 3.0f;      // 최대 감소까지 걸리는 시간 (3초)
        [SerializeField] private float _damageRampUpTime = 3.0f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("시각 이펙트")]
        [SerializeField] private GameObject _auraVfxPrefab;     // 아우라 원형 이펙트 프리팹
        
        [Header("쿨타임 설정")]
        [SerializeField] private float auraCoolTime = 6.0f;

        public override void OnSkillUsed(AugmentContext ctx)
        {
            // 중복 실행 방지 플래그 체크 (이미 살기 아우라가 켜져 있으면 리턴)
            if (ctx.IsOnCooldown(type) || ctx.GetFlag(type)) return;

            // 코루틴 실행
            ctx.Runner.StartCoroutine(Co_KillingIntentAura(ctx));
        }

        private IEnumerator Co_KillingIntentAura(AugmentContext ctx)
        {
            ctx.SetFlag(type, true);

            // 1. 아우라 VFX 생성 및 플레이어 자식으로 부착 (따라다니도록)
            GameObject vfx = null;
            if (_auraVfxPrefab != null && ctx.Owner != null)
            {
                vfx = Instantiate(_auraVfxPrefab, ctx.Owner.transform);
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localScale = Vector3.one * (_radius * 2f); // 반지름 3m = 지름 6m
            }

            float elapsedTime = 0f;
            float tickInterval = 0.1f; // 0.1초마다 디버프 갱신
            var wait = new WaitForSeconds(tickInterval);

            // 2. 6초 동안 아우라 유지
            while (elapsedTime < _duration)
            {
                // 시전자(플레이어)가 파괴되거나 사망하면 중단
                if (ctx.Owner == null) break;

                elapsedTime += tickInterval;

                // 3초에 걸쳐 0%에서 30%까지 점진적으로 증가 (3초 이후는 30% 유지)
                float currentDebuffRatio = Mathf.Min(_maxSpeedDebuffRatio,
                    (elapsedTime / _speedRampUpTime) * _maxSpeedDebuffRatio);
                float currentDamageDebuffRatio = Mathf.Min(_maxDamageDebuffRatio,
                    (elapsedTime / _damageRampUpTime) * _maxDamageDebuffRatio);

                // 3. 반경 3m 내 적 탐색
                Collider2D[] enemies = Physics2D.OverlapCircleAll(ctx.Owner.transform.position, _radius, _enemyLayer);

                foreach (var enemy in enemies)
                {
                    if(enemy.gameObject == ctx.Owner) continue;
                    
                    // 이동 속도 감쇄 (틱 간격보다 조금 길게 0.2초 유지)
                    if (enemy.TryGetComponent<ISlowable>(out var speedComp))
                    {
                        speedComp.ApplySlow(currentDebuffRatio, 0.2f);
                    }
                    
                    if (enemy.TryGetComponent(out IWeakenable weakenable))
                    {
                        weakenable.ApplyAttackWeaken(currentDamageDebuffRatio, 0.2f);
                    }
                }

                yield return wait;
            }

            // 4. 정리 (VFX 삭제 및 플래그 해제)
            if (vfx != null) Destroy(vfx);
            ctx.SetFlag(type, false);
            ctx.SetCooldown(type, auraCoolTime);
        }
    }
}