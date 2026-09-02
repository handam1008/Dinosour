using System.Collections.Generic;
using KDH.Scripts.Upgrade;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Player
{
    public class GunnerAugmentController : AugmentReceiverBehaviour
    {
        public override PlayerJob Job => PlayerJob.Gunner;
    
        readonly HashSet<GunnerAugmentType> _has = new();
        
        [SerializeField] private KDH_BulletAbilityDataSO _bulletData;
        private KDH_UpgradeList upgradeList;

        private void Awake()
        {
            upgradeList = GetComponentInChildren<KDH_UpgradeList>();
        }
        
        public override bool TryReceive(Augment augment)
        {
            if (augment is not GunnerArgument w) return false;
            _has.Add(w.type);
            return true;
        }
    
        public bool Has(GunnerAugmentType t) => _has.Contains(t);
        
        // 게임 코드가 읽어갈 값. 없으면 1배(= 효과 없음)
        
        public float SplashMultiplier => Has(GunnerAugmentType.IceBullet) ? 1.3f : 1f;
    }
}
