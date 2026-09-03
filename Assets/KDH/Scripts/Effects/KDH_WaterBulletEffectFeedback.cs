using System;
using KDH.Scripts.Bullet;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances;
using UnityEngine;

public class KDH_WaterBulletEffect : KDH_AbstractFeedback
{
    private KDH_WaterBullet _parent;
    private GameObject effect;
    private KDH_Bullet _bullet;

    private void Start()
    {
        _parent = GetComponentInParent<KDH_WaterBullet>();
    }

    public override void CreateFeedBack()
    {
        _bullet = _parent.Bullet;
        
        if (_bullet.IsUpgraded)
        {
            effect = Instantiate(_parent.BulletAbilityData.bulletUpgradedEffectPrefab, _parent.HitPoint.position, 
                Quaternion.identity);
            Destroy(effect, 1.5f);
        }
        else
        {
            effect = Instantiate(_parent.BulletAbilityData.bulletNormalEffectPrefab,  _parent.HitPoint.position,
                Quaternion.identity);
            Destroy(effect, 1.5f);
        }
    }

    public override void StopFeedBack()
    {
        
    }
}
