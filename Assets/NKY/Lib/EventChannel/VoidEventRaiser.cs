using System;
using UnityEngine;

namespace NKY.Lib.EventChannel
{
    public class VoidEventRaiser : EventRaiserBase
    {
        [SerializeField] private VoidEventChannelSO channel;

        public override void Raise()
        {
            // 여기서 channel의 Raise() 호출
            if (channel == null)
            {
                Debug.LogError("channel is null");
                return;
            }

            channel.Raise();
        }
    }
}