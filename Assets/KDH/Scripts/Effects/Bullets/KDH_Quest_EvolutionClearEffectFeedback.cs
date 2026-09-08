using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_Quest_EvolutionClearEffectFeedback : KDH_AbstractFeedback
    {
        [SerializeField] private GameObject clearEffect;

        private KDH_Quest_EvolutionBullet _parent;
        
        private void Awake()
        {
            _parent = GetComponentInParent<KDH_Quest_EvolutionBullet>();
        }

        public override void CreateFeedBack()
        {
            Instantiate(clearEffect, _parent.Bullet.PlayerGun.transform); // 위치 바꿔야함
        }

        public override void StopFeedBack()
        {
            
        }
    }
}
