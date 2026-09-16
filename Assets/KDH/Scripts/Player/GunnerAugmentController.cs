using System;
using System.Collections.Generic;
using KDH.Scripts.Arguments;
using KDH.Scripts.Upgrade;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Player
{
    public class GunnerAugmentController : AugmentReceiverBehaviour
    {
        public override PlayerJob Job => PlayerJob.Gunner;

        [SerializeField] private KDH_UpgradeList upgradeList;
        
        readonly HashSet<GunnerAugmentType> _has = new();

        [Serializable]
        private struct AugmentAbilityMapping
        {
            public GunnerAugmentType type;
            public KDH_BulletAbilityDataSO bulletAbilityData;
            public KDH_PlayerAbilitySO playerAbilityData;
        }

        [SerializeField] private List<AugmentAbilityMapping> augmentMappings = new();

        public override bool TryReceive(Augment augment)
        {
            if (augment is not GunnerArgument w) return false;

            _has.Add(w.type);

            var mapping = augmentMappings.Find(m => m.type == w.type);

            if (mapping.bulletAbilityData != null)
                upgradeList.AddBulletAbility(mapping.bulletAbilityData);

            if (mapping.playerAbilityData != null)
                upgradeList.AddPlayerAbility(mapping.playerAbilityData);

            return true;
        }
    }
}