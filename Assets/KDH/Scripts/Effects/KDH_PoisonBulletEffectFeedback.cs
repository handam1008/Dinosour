using KDH.Scripts.Effects;
using KDH.Scripts.System.FeedbackSystem;
using UnityEngine;

public class KDH_PoisonBulletEffectFeedback : KDH_AbstractFeedback
{
    private Transform _target;

    private KDH_PoisonBullet _parent;

    private GameObject effect;

    private KDH_FollowPlayerEffect followModule;
    
    private void Awake()
    {
        _parent =  GetComponentInParent<KDH_PoisonBullet>();
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
            effect.GetComponent<KDH_PosionBlinkEffect>().SetBlink(_parent.PoisonDuration / _parent.DotCount, _parent.PoisonDuration / _parent.DotCount);
                
            followModule = effect.GetComponent<KDH_FollowPlayerEffect>();
            followModule.Init(_target);
            
            Destroy(effect, _parent.PoisonDuration);
        }
        else
        {
            effect = Instantiate(_parent.BulletAbilityData.bulletNormalEffectPrefab, _target.position,
                Quaternion.identity);
            effect.GetComponent<KDH_PosionBlinkEffect>().SetBlink(_parent.PoisonDuration / _parent.DotCount, _parent.PoisonDuration / _parent.DotCount);
                
            followModule = effect.GetComponent<KDH_FollowPlayerEffect>();
            followModule.Init(_target);
            
            Destroy(effect, _parent.PoisonDuration);
        }
    }

    public override void StopFeedBack()
    {
        
    }
}
