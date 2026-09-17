using NKY.Scripts.Job;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Skill
{
    [CreateAssetMenu(fileName = "skillData", menuName = "SKill/Assassin/normal", order = 0)]
    public class AssassinNormalSkillSo : AbstractPlayerSkillSo
    {
        [field: SerializeField] public float Damage { get; private set; }
        [field: SerializeField] public float ThrowSpeed { get; private set; }
        [field: SerializeField] public LayerMask WhatIsTarget { get; private set; }
        [field: SerializeField] public float DestroyTime { get; private set; }
        [SerializeField] private AssassinNormalSkill _skillPrefab;
        
        public override GameObject ExecuteSkill(PlayerController player, Vector3 aimDirection)
        {
            AssassinNormalSkill projectile = Instantiate(_skillPrefab, player.transform.position, Quaternion.identity);
            projectile.Init(player, Damage, ThrowSpeed, WhatIsTarget, DestroyTime);
            projectile.transform.up = aimDirection;
            
            projectile.Launch(aimDirection);
            
            var augmentController = player.GetComponentInChildren<AssassinAugmentController>();
            if (augmentController != null)
            {
                augmentController.NotifySpawnProjectile(projectile.gameObject);
            }
            
            

            return projectile.gameObject;
        }

        public override void ExecuteReUseSkill(PlayerController player, GameObject activeSkillInstance)
        {
            if (activeSkillInstance == null) return;

            if (activeSkillInstance.TryGetComponent<AssassinNormalSkill>(out var skillProjectile))
            {
                // 위치 이동 및 밀쳐내기
                player.transform.position = skillProjectile.transform.position;
                
                Vector3 direction = skillProjectile.transform.up.normalized;
                if (player.TryGetComponent<IForceReceiver>(out var forceReceiver))
                {
                    forceReceiver.ApplyForce(skillProjectile.Rb.linearVelocity.magnitude * direction, ForceMode2D.Impulse);
                }

                Destroy(skillProjectile.gameObject);
            }
        }
        
        
    }
}