using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NKY.Lib.EventChannel
{
    [Serializable]
    public class VoidChannelEntry
    {
        public VoidEventChannelSO channel;
        public UnityEvent response;

        [NonSerialized]
        public Action onCachedHandler;
    }
    
    public class VoidEventListener : EventListenerBase
    {
        [SerializeField] private List<VoidChannelEntry> eventChannels;
        
        private void OnEnable()
        {
            if (eventChannels.Count == 0)
            {
                Debug.LogError($"[EventChannel] No EventChannel found for {name}");
                return;
            }

            foreach (VoidChannelEntry entry in eventChannels)
            {
                if (entry.channel == null || entry.response == null)
                {
                    Debug.LogError($"[EventChannel] No response found for {entry.channel.name}, {entry.response}");
                    return;
                }
                entry.onCachedHandler = () => entry.response.Invoke();
                entry.channel.OnEventRaised += entry.onCachedHandler;
            }
        }

        private void OnDisable()
        {
            if (eventChannels.Count == 0) return;
            
            foreach (VoidChannelEntry entry in eventChannels)
            {
                if (entry.channel == null || entry.response == null) return;
                
                entry.channel.OnEventRaised -= entry.onCachedHandler;
                entry.onCachedHandler = null;
            }
        }
    }
}