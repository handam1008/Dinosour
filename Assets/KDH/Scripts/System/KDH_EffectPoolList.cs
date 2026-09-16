using System.Collections.Generic;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade;
using UnityEngine;

namespace KDH.Scripts.System
{
    [CreateAssetMenu(fileName = "KDH_EffectPoolListSO", menuName = "KDH/EffectPoolingListSO", order = 0)]
    public class KDH_EffectPoolList : ScriptableObject
    {
        public List<KDH_BulletAbilityDataSO> list;
    }
}