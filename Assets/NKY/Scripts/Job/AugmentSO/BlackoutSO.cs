using NKY.Scripts.Servers;
using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "Blackout", menuName = "Player/job/Augment/Assassin/Blackout", order = 0)]
    public class BlackoutSO : AbstractAssassinAugmentSO
    {
        [Header("증강 구현 설정")]
        [SerializeField] private float blackoutDuration = 2.5f; // 시야 차단 지속시간
        
        [Header("쿨타임 설정")]
        [SerializeField] private float coolTime = 15f;
        
        public override void OnSkillUsed(AugmentContext ctx)
        {
            // 쿨타임 또는 이미 실행 중인지 체크
            if (ctx.IsOnCooldown(type) || ctx.GetFlag(type)) return;

            if (ctx.Owner.TryGetComponent<BlackoutNetwork>(out var blackoutNet))
            {
                // 쿨타임 등록
                ctx.SetCooldown(type, coolTime +  blackoutDuration);

                // 암전 네트워크 트리거 호출
                blackoutNet.TriggerBlackout(blackoutDuration);
            }
        }
    }
}