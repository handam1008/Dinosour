using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKY.Scripts.Job
{
    [Serializable]
    enum Job
    {
        Sword,
        Assassin,
        Gambler,
        Magician,
        Gunner,
        Witch
    }
    [CreateAssetMenu(fileName = "jobData", menuName = "Player/JobData", order = 0)]
    public class PlayerJobDataSo : ScriptableObject
    {
        [SerializeField] private Job job;
        
        [SerializeField] private List<AbstractJobAugmentDataSo> augmentList;
    }
}