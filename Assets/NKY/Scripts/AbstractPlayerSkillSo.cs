using UnityEngine;
using System.Collections;
using NKY.Lib.EventChannel;
using SSW;

namespace NKY.Scripts
{
    public abstract class AbstractPlayerSkillSo : ScriptableObject
    {
        [field: SerializeField] public float SkillCooldown { get; protected set; }
        [field: SerializeField] public bool IsReUse { get; protected set; }

        /// <summary>
        /// 스킬 최초 실행 로직
        /// </summary>
        /// <param name="player">스킬 시전자</param>
        /// <param name="aimDirection">조준 방향</param>
        /// <returns>재사용 스킬에 필요한 생성된 인스턴스 (없으면 null)</returns>
        public abstract GameObject ExecuteSkill(PlayerController player, Vector3 aimDirection);

        /// <summary>
        /// 스킬 재사용 로직 (위치 이동, 폭발 등)
        /// </summary>
        public virtual void ExecuteReUseSkill(PlayerController player, GameObject activeSkillInstance)
        {
        }
    }
}