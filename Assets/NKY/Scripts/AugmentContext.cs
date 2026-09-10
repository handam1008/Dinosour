using System;
using System.Collections.Generic;
using NKY.Scripts.Job;
using UnityEngine;

namespace NKY.Scripts
{
    public class AugmentContext
    {
        public GameObject Owner { get; }

        // 2. SO 내부에서 코루틴(StartCoroutine)을 실행할 때 빌려 쓸 MonoBehaviour (Controller)
        public MonoBehaviour Runner { get; }

        // 3. 플레이어 개별 쿨타임 저장소 (증강타입, 쿨타임 종료 시각)
        private readonly Dictionary<AssassinAugmentType, float> _cooldowns = new();
        
        private readonly HashSet<AssassinAugmentType> _flags = new();

        public AugmentContext(GameObject owner, MonoBehaviour runner)
        {
            Owner = owner;
            Runner = runner;
            
            Init();
        }

        private void Init()
        {
            foreach (AssassinAugmentType type in Enum.GetValues(typeof(AssassinAugmentType)))
            {
                SetCooldown(type, 0f);
            }
        }
        
        // 쿨타임 확인 함수
        public bool IsOnCooldown(AssassinAugmentType type)
        {
            return _cooldowns.TryGetValue(type, out var expireTime) && Time.time < expireTime;
        }

        // 쿨타임 설정 함수
        public void SetCooldown(AssassinAugmentType type, float duration)
        {
            _cooldowns[type] = Time.time + duration;
        }
        
        public void SetFlag(AssassinAugmentType type, bool active)
        {
            if (active) _flags.Add(type);
            else _flags.Remove(type);
        }

        // 플래그(버프) 켜져 있는지 확인
        public bool GetFlag(AssassinAugmentType type) => _flags.Contains(type);
    }
}