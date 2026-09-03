using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NKY.Lib.EventChannel
{
    [Serializable]
    public abstract class ChannelEntry<T>
    {
        public EventChannelSO<T> channel;

        [NonSerialized]
        public Action<T> onCachedHandler;

        // response가 뭔지는 몰라도, "값 받으면 실행해라"는 요구할 수 있음
        public abstract void InvokeResponse(T value);
    }
    
    public abstract class EventListener<T, TEntry> : EventListenerBase where TEntry : ChannelEntry<T>
    {
        [SerializeField] protected List<TEntry> eventChannels;
        
        private void OnEnable()
        {
            if (eventChannels.Count == 0)
            {
                Debug.LogError($"[EventChannel] No EventChannel found for {name}");
                return;
            }

            foreach (ChannelEntry<T> entry in eventChannels)
            {
                if (entry.channel == null)
                {
                    Debug.LogError($"[EventChannel] No response found for {entry.channel.name}");
                    return;
                }
                entry.onCachedHandler = (value) => entry.InvokeResponse(value);
                entry.channel.OnEventRaised += entry.onCachedHandler;
            }
        }

        private void OnDisable()
        {
            if (eventChannels.Count == 0) return;
            
            foreach (ChannelEntry<T> entry in eventChannels)
            {
                if (entry.channel == null) return;
                
                entry.channel.OnEventRaised -= entry.onCachedHandler;
                entry.onCachedHandler = null;
            }
        }
    }
}