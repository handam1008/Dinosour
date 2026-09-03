using System;
using KDH.Scripts.Effects;
using KDH.Scripts.System.FeedbackSystem;
using UnityEngine;

public class KDH_FireBulletEffectFeedback : KDH_AbstractFeedback
{
    private Transform _target;

    private KDH_FireBullet _parent;

    private GameObject effect;

    private KDH_FollowPlayerEffect followModule;
    
    private void Awake()
    {
        _parent =  GetComponentInParent<KDH_FireBullet>();
    }

    private void Update()
    {
        if (followModule != null && effect != null)
            followModule.MoveEffect = effect.activeSelf;
    }

    public override void CreateFeedBack()
    {
        _target = _parent.HitPoint;
            
        if (_target == null)
            return;

        if (_parent.Bullet.IsUpgraded)
        {
            effect = Instantiate(_parent.BulletAbilityData.bulletUpgradedEffectPrefab, _target.position,
                Quaternion.identity);
                
            followModule = effect.GetComponent<KDH_FollowPlayerEffect>();
            followModule.Init(_target);

            
            Destroy(effect, _parent.FireireDuration);
        }
        else
        {
            effect = Instantiate(_parent.BulletAbilityData.bulletNormalEffectPrefab, _target.position,
                Quaternion.identity);
                
            followModule = effect.GetComponent<KDH_FollowPlayerEffect>();
            followModule.Init(_target);
            
            Destroy(effect, _parent.FireireDuration);
        }
    }

    public override void StopFeedBack()
    {
        
    }
}
