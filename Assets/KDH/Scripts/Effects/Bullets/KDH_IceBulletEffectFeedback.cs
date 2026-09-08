using System.Collections;
using KDH.Scripts.System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_IceBulletEffectFeedback : KDH_AbstractFeedback
    {
        private float effectDuration = 1.5f;
        private Transform _target;
        private GameObject _effect;
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
            
            GameObject prefab = _parent.Bullet.IsUpgraded
                ? _parent.BulletAbilityData.bulletUpgradedEffectPrefab
                : _parent.BulletAbilityData.bulletNormalEffectPrefab;

            _effect = KDH_EffectPoolManager.Instance.Get(prefab, _target.position, Quaternion.identity);
            
            StartCoroutine(ReleaseAfter(prefab, _effect, effectDuration));
        }
        
            private IEnumerator ReleaseAfter(GameObject prefab, GameObject effect, float duration)
            {
                yield return new WaitForSeconds(duration);

                if (effect != null)
                    KDH_EffectPoolManager.Instance.Release(prefab, effect);
            }
        
        public override void StopFeedBack()
        {
            
        }
    }
}