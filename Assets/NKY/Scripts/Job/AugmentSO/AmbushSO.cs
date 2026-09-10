using System.Collections;
using NKY.Scripts.Servers;
using SSW;
using UnityEngine;
using UnityEngine.UI;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "Ambush", menuName = "Player/job/Augment/Assassin/Ambush", order = 0)]
    public class AmbushSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private float bushDuration = 0.5f;
        
        [Header("쿨타임 설정")]
        [SerializeField] private float coolTime = 7f;
        public override void OnBasicAttackHit(AugmentContext ctx, PlayerController target)
        {
            if (ctx.IsOnCooldown(type)) return;

            if (ctx.Owner.TryGetComponent<StealthNetwork>(out var stealthNet))
            {
                ctx.SetCooldown(type, coolTime + bushDuration);
                stealthNet.TriggerStealth(bushDuration);
            }
        }
    }
}