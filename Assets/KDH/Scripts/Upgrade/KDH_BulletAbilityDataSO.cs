using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    [CreateAssetMenu(fileName = "SO", menuName = "KDH/Ability", order = 0)]
    public class KDH_BulletAbilityDataSO : ScriptableObject
    {
        public string abilityName;
        [TextArea] public string abilityDescription;
        
        public int damage;
        
        public GameObject bulletAbilityPrefab;
        public GameObject bulletNormalEffectPrefab;
        public GameObject bulletUpgradedEffectPrefab;
        public GameObject bulletTrailEffectPrefab;
    }
}