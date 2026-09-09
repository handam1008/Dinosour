using System.Collections;
using KDH.Scripts.System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_FireBulletEffectFeedback : KDH_AbstractFeedback
    {
        private GameObject _effect;
        private GameObject _bulletTrailEffect;
        private Transform _target;
        private KDH_FireBullet _parent;
        private KDH_FollowPlayerEffect _followModule;
        
        private void Awake()
        {
            _parent = GetComponentInParent<KDH_FireBullet>();
        }
        
        private void Update()
        {
            if (_followModule != null && _effect != null)
                _followModule.MoveEffect = _effect.activeSelf;
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

            // GameObject trailEffect = _parent.BulletAbilityData.bulletTrailEffectPrefab;
            // _bulletTrailEffect = KDH_EffectPoolManager.Instance.Get(trailEffect, _parent.Bullet.gameObject.transform.position, Quaternion.identity);
            // KDH_EffectPoolManager.Instance.Release(trailEffect, _bulletTrailEffect);

            _followModule = _effect.GetComponent<KDH_FollowPlayerEffect>();

            if (_followModule != null)
                _followModule.Init(_target);

            StartCoroutine(ReleaseAfter(prefab, _effect, _parent.FireireDuration));
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