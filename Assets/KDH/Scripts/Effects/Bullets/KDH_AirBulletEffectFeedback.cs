using System.Collections;
using KDH.Scripts.System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_AirBulletEffectFeedback : KDH_AbstractFeedback
    {
        private float effectDuration = 1.5f;
        private GameObject _effect;
        private GameObject _bulletTrailEffect;
        private Transform _target;
        private KDH_AirBullet _parent;
        
        private void Awake()
        {
            _parent = GetComponentInParent<KDH_AirBullet>();
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

        public override void StopFeedBack()
        {
        
        }
        
        private IEnumerator ReleaseAfter(GameObject prefab, GameObject effect, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (effect != null)
                KDH_EffectPoolManager.Instance.Release(prefab, effect);
        }
    }
}
