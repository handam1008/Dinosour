using UnityEngine;
using System.Collections;

namespace NKY.Scripts
{
    public abstract class AbstractPlayerSkillSo : ScriptableObject
    {
        [field: SerializeField] public float SkillCooldown {get; protected set;}
        
        [field: SerializeField] public bool IsReUse {get; protected set;}

        private bool _skillReUse = false;
        private float _currentCooldown;

        public virtual void Init()
        {
            _currentCooldown = -99f;
        }

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

            if (Time.time - _currentCooldown >= SkillCooldown)
            {
                UseSkill(player);
                if (IsReUse)
                    _skillReUse = true;
                _currentCooldown = Time.time;
            }
        }

        protected abstract IEnumerator SkillCoroutine(TestJump player);

        protected virtual IEnumerator ReUseSkillCoroutine(TestJump player)
        {
            yield break;
        }
    }
}