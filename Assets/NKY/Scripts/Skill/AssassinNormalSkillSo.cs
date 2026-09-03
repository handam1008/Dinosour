using System.Collections;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKY.Scripts.Skill
{
    [CreateAssetMenu(fileName = "skillData", menuName = "SKill/Assassin/normal", order = 0)]
    public class AssassinNormalSkillSo : AbstractPlayerSkillSo
    {
        [field: SerializeField] public float Damage {get; private set;}
        [field: SerializeField] public float ThrowSpeed {get; private set;}
        [field: SerializeField] public LayerMask WhatIsTarget {get; private set;}
        [field: SerializeField] public float DestroyTime {get; private set;}
        [SerializeField] private AssassinNormalSkill skillPrefab;


        private AssassinNormalSkill _skill;
        
        protected override IEnumerator SkillCoroutine(PlayerController player)
        {
            if (Camera.main != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
                mouseWorldPos.z = 0; 
            
                Vector3 direction = mouseWorldPos - player.transform.position;
            
                direction.Normalize();
                _skill = Instantiate(skillPrefab, player.transform.position, Quaternion.identity);
                _skill.Init(player, Damage, ThrowSpeed, WhatIsTarget, DestroyTime);
                _skill.transform.up = direction;
            }

            yield break;
        }

        protected override IEnumerator ReUseSkillCoroutine(PlayerController player)
        {
            if (_skill == null) yield break;
            player.transform.position = _skill.transform.position;
            Vector3 direction = _skill.transform.up.normalized;
            IForceReceiver forceReceiver = player.GetComponent<IForceReceiver>();
            forceReceiver.ApplyForce(_skill.Rb.linearVelocity.magnitude * direction, ForceMode2D.Impulse);
            Destroy(_skill.gameObject);
            _skill = null;
        }
        
        
    }
}