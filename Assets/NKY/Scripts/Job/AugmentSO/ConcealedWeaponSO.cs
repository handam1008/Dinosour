using UnityEngine;

namespace NKY.Scripts.Job.AugmentSO
{
    [CreateAssetMenu(fileName = "ConcealedWeapon", menuName = "Player/job/Augment/Assassin/ConcealedWeapon", order = 0)]
    public class ConcealedWeaponSO : AbstractAssassinAugmentSO
    {
        [Header("증강 효과 설정")]
        [SerializeField] private int bounceCount = 1;
        
        public override void OnSpawnProjectile(AugmentContext ctx, GameObject projectile)
        {
            // 필요 시 ctx.Owner로 시전자를 확인하거나 쿨타임/플래그를 검사할 수 있습니다.
            if (projectile.TryGetComponent<AssassinNormalSkill>(out var dagger))
            {
                dagger.SetMaxBounces(bounceCount);
            }
        }
    }
}