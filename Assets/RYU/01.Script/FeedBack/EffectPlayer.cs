using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.FeedBack
{
    public class EffectPlayer : AbstractFeedBack
    {
        [SerializeField] private GameObject _glassEffect;
        public override void CreateFeedBack(Vector3 pos)
        {
            GameObject effectGo = Instantiate(_glassEffect , pos, Quaternion.identity);
            ParticleSystem effect = effectGo.GetComponent<ParticleSystem>();
            effect.Play();
        }

        

        public override void StopFeedBack()
        {
            
        }

        public void hit(Transform hit)
        {
            
        }
    }
}