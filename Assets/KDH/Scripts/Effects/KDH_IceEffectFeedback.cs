using System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade;
using KDH.Scripts.Upgrade.Instances;
using UnityEngine;

namespace KDH.Scripts.Effects
{
    public class KDH_IceEffectFeedback : KDH_AbstractFeedback
    {
        [SerializeField] private KDH_BulletAbilityDataSO iceEffect;

        private Transform _targetTrm;

        private KDH_IceBullet _parent;

        private void Awake()
        {
            _parent = GetComponentInParent<KDH_IceBullet>();
        }

        public override void CreateFeedBack()
        {
            Transform targetTrm = _parent.HitPoint;

            if (targetTrm == null)
                return;

            if (_parent.Bullet.IsUpgraded)
            {
                GameObject effect = Instantiate(iceEffect.BulletUpgradedEffectPrefab, targetTrm.position,
                    Quaternion.identity);
                Destroy(effect, 1.5f);
            }
            else
            {
                GameObject effect = Instantiate(iceEffect.BulletNormalEffectPrefab, targetTrm.position,
                    Quaternion.identity);
                Destroy(effect, 1.5f);
            }
        }

        public override void StopFeedBack()
        {
            
        }
    }
}