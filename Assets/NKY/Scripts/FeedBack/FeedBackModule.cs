using System;
using System.Collections.Generic;
using System.Linq;
using NKY.Lib.EventChannel;
using SSW;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    public class FeedBackModule : MonoBehaviour
    {
        private VoidEventChannelSO _feedBackEventChannel;
        private List<AbstractFeedBack> _feedBacks;

        private void Awake()
        {
            
        }

        private void Start()
        {
            _feedBackEventChannel = GetComponentInParent<Health>().HitEvent;
            if(_feedBackEventChannel == null) { Debug.LogError("Feed Back Channel Not Found"); return; } 
            
            _feedBackEventChannel.OnEventRaised += FeedBack;
            _feedBacks = GetComponentsInChildren<AbstractFeedBack>().ToList();
            Debug.Assert(_feedBacks.Count >= 0, "not equip feedback");
        }

        private void FeedBack()
        {
            foreach (var feedBack in _feedBacks)
            {
                feedBack.OnFeedBack();
            }
        }
    }
}