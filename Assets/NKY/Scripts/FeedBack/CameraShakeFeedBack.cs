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
        private SandboxCameraFollow _impulseSource;

        private void Awake()
        {
            if(Camera.main != null)
                _impulseSource = Camera.main.GetComponent<SandboxCameraFollow>();
        }

        public override void OnFeedBack()
        {
            if(_impulseSource == null) return;
            
            _impulseSource.Shake(force, duration);
        }
    }
}