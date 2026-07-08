using UnityEngine;
using System.Collections;

namespace NKY.Scripts
{
    public abstract class AbstractPlayerSkillSo : ScriptableObject
    {
        [field: SerializeField] public float SkillCooldown {get; protected set;}
        
        [field: SerializeField] public bool IsReUse {get; protected set;}
        
        private float _cooldown;
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
            Debug.Log("Starting skill");
            if (IsReUse && _skillReUse)
            {
                ReUseSkill(player);
                return;
            }
            if (Time.time - _cooldown >= SkillCooldown)
            {
                UseSkill(player);
            }
            _cooldown = Time.time;
        }

        protected abstract IEnumerator SkillCoroutine(TestJump player);

        protected virtual IEnumerator ReUseSkillCoroutine(TestJump player)
        {
            yield break;
        }
    }
}