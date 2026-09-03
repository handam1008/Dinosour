using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances;
using UnityEngine;

namespace KDH.Scripts.Effects
{
    public class KDH_IceBulletEffectFeedback : KDH_AbstractFeedback
    {
        private Transform _target;

        private KDH_IceBullet _parent;
        
        private void Awake()
        {
            _parent = GetComponentInParent<KDH_IceBullet>();
        }

        public override void CreateFeedBack()
        {
            _target = _parent.HitPoint;
            
            if (_target == null)
                return;

            if (_parent.Bullet.IsUpgraded)
            {
                GameObject effect = Instantiate(_parent.BulletAbilityData.bulletUpgradedEffectPrefab, _target.position,
                    Quaternion.identity);
                
                Destroy(effect, 1.5f);
            }
            else
            {
                GameObject effect = Instantiate(_parent.BulletAbilityData.bulletNormalEffectPrefab, _target.position,
                    Quaternion.identity);
                
                Destroy(effect, 1.5f);
            }
        }

        public override void StopFeedBack()
        {
            
        }
    }
}