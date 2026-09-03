using System;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKY.Scripts.Skill
{
    public class PlayerSkillModule : MonoBehaviour
    {
        [SerializeField] private AbstractPlayerSkillSo skill;
        
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            
            skill.Init();
        }

        private void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                skill.StartSkill(_player);
            }
        }
    }
}