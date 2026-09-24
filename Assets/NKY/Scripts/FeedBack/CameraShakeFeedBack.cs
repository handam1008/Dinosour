using System;
using Unity.Cinemachine;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeFeedBack : AbstractFeedBack
    {
        [SerializeField] private float force;
        private CinemachineImpulseSource _impulseSource;

        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        public override void OnFeedBack()
        {
            _impulseSource.GenerateImpulse(force);
        }
    }
}