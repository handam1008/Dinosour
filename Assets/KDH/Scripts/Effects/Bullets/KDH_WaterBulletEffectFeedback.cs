using System.Collections;
using KDH.Scripts.System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_ShurikenBulletEffect : KDH_AbstractFeedback
    {
        private KDH_ShurikenBullet _parent;
        private GameObject _effect;
        private Transform _target;
        private float _effectDuration = 1.5f;
    
        private void Start()
        {
            _parent = GetComponentInParent<KDH_ShurikenBullet>();
        }

        public override void CreateFeedBack()
        {
            _target = _parent.HitPoint;
        
            GameObject prefab = _parent.Bullet.IsUpgraded
                ? _parent.BulletAbilityData.bulletUpgradedEffectPrefab
                : _parent.BulletAbilityData.bulletNormalEffectPrefab;

            _effect = KDH_EffectPoolManager.Instance.Get(prefab, _target.position, Quaternion.identity);

            StartCoroutine(ReleaseAfter(prefab, _effect, _effectDuration));
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
