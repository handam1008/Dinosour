using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    [CreateAssetMenu(fileName = "PlayerAbilitySO", menuName = "KDH/PlayerAbility", order = 0)]
    public class KDH_PlayerAbilitySO : ScriptableObject
    {
        public string abilityName;
        [TextArea] public string abilityDescription;
        
        public GameObject playerAbilityPrefab;
    }
}