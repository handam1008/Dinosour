using UnityEngine;
using System.Collections;
using SSW;

namespace NKY.Scripts
{
    public abstract class AbstractPlayerSkillSo : ScriptableObject
    {
        [field: SerializeField] public float SkillCooldown {get; protected set;}
        
        [field: SerializeField] public bool IsReUse {get; protected set;}

        private bool _skillReUse = false;
        private float _currentCooldown;

        public float CurrentCooldown
        {
            get
            {
                return _currentCooldown;
            }

            private set
            {
                _currentCooldown = value;
            }
        }

        public virtual void Init()
        {
            CurrentCooldown = -99f;
        }

        private void UseSkill(PlayerController player)
        {
            player.StartCoroutine(SkillCoroutine(player));
        }

        private void ReUseSkill(PlayerController player)
        {
            player.StartCoroutine(ReUseSkillCoroutine(player));
        }

        public void StartSkill(PlayerController player)
        {
            if (IsReUse && _skillReUse)
            {
                _skillReUse = false;
                ReUseSkill(player);
                return;
            }
            
            if (Time.time - CurrentCooldown >= SkillCooldown)
            {
                UseSkill(player);
                if (IsReUse)
                    _skillReUse = true;
                CurrentCooldown = Time.time;
            }
        }

        protected abstract IEnumerator SkillCoroutine(PlayerController player);

        protected virtual IEnumerator ReUseSkillCoroutine(PlayerController player)
        {
            yield break;
        }
    }
}