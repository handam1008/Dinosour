using UnityEngine;
using System.Collections;

namespace NKY.Scripts
{
    public abstract class AbstractPlayerSkillSo : ScriptableObject
    {
        [field: SerializeField] public float SkillCooldown {get; protected set;}
        
        [field: SerializeField] public bool IsReUse {get; protected set;}
        
        [SerializeField] private float cooldown;
        private bool _skillReUse;
        

        private void UseSkill(TestJump player)
        {
            player.StartCoroutine(SkillCoroutine(player));
        }

        private void ReUseSkill(TestJump player)
        {
            player.StartCoroutine(ReUseSkillCoroutine(player));
        }

        public void StartSkill(TestJump player)
        {
            if (IsReUse && _skillReUse)
            {
                _skillReUse = false;
                ReUseSkill(player);
                return;
            }

            if (Time.time - cooldown >= SkillCooldown)
            {
                UseSkill(player);
                if (IsReUse)
                    _skillReUse = true;
                cooldown = Time.time;
            }
        }

        protected abstract IEnumerator SkillCoroutine(TestJump player);

        protected virtual IEnumerator ReUseSkillCoroutine(TestJump player)
        {
            yield break;
        }
    }
}