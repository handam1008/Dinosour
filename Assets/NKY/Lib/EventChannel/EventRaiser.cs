using UnityEngine;

namespace NKY.Lib.EventChannel
{
    public abstract class EventRaiser<T> : EventRaiserBase
    {
        private static readonly bool IsValueType = typeof(T).IsValueType;
        
        [SerializeField] private EventChannelSO<T> channel;
        [SerializeField] private T value;

        public override void Raise()
        {
            if (channel == null || (!IsValueType && value == null))
            {
                Debug.LogError("channel is null");
                return;
            }
            
            channel.Raise(value);
        }
    }
}