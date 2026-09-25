using System;
using SSW;
using Unity.Cinemachine;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    public class CameraShakeFeedBack : AbstractFeedBack
    {
        [SerializeField] private float force;
        [SerializeField] private float duration;
        private ICameraShakeReceiver _impulseSource;

        private void Awake()
        {
            if (Camera.main != null)
            {
                if (Camera.main.TryGetComponent(out _impulseSource)) ;
            }
        }

        public override void OnFeedBack()
        {
            if(_impulseSource == null) return;
            
            _impulseSource.Shake(force, duration);
        }
    }
}