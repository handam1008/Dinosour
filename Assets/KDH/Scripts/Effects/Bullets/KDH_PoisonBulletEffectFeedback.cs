using System.Collections;
using KDH.Scripts.System;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_PoisonBulletEffectFeedback : KDH_AbstractFeedback
    {
        private Transform _target;
        private KDH_PoisonBullet _parent;
        private GameObject _effect;
        private KDH_FollowPlayerEffect _followModule;
    
        private void Awake()
        {
            _parent =  GetComponentInParent<KDH_PoisonBullet>();
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

            _followModule = _effect.GetComponent<KDH_FollowPlayerEffect>();
            // _effect.GetComponent<KDH_PosionBlinkEffect>().SetBlink(_parent.PoisonDuration / _parent.DotCount, _parent.PoisonDuration / _parent.DotCount);
            foreach (KDH_PosionBlinkEffect blinkeffect in _parent.Bullet.PlayerGun.EffectPoolList.gameObject
                         .GetComponentsInChildren<KDH_PosionBlinkEffect>())
            {
                blinkeffect.GetComponent<ParticleSystem>().Stop();
            }
            _effect.GetComponent<KDH_PosionBlinkEffect>().SetBlink(_parent.PosionTickInterval, _parent.PosionTickInterval);

            if (_followModule != null)
                _followModule.Init(_target);

            StartCoroutine(ReleaseAfter(prefab, _effect, _parent.PoisonDuration));
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
