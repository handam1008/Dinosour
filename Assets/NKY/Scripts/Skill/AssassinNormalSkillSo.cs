using System.Collections;
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
        protected override IEnumerator SkillCoroutine(TestJump player)
        {
            Debug.Log("use");
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            mouseWorldPos.z = 0; 
            
            Vector3 direction = mouseWorldPos - player.transform.position;
            
            direction.Normalize();
            _skill = Instantiate(skillPrefab, player.transform.position, Quaternion.identity);
            _skill.Init(Damage, ThrowSpeed, WhatIsTarget, DestroyTime);
            _skill.transform.up = direction;
            
            yield break;
        }

        protected override IEnumerator ReUseSkillCoroutine(TestJump player)
        {
            Debug.Log("reuse");
            if (_skill == null) yield break;
            Debug.Log("reuse 찐또");
            player.transform.position = _skill.transform.position;
            Destroy(_skill.gameObject);
            _skill = null;
        }
    }
}