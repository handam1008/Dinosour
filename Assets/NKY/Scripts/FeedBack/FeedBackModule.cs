using System;
using System.Collections.Generic;
using System.Linq;
using NKY.Lib.EventChannel;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    public class FeedBackModule : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO feedBackEventChannel;

        private List<AbstractFeedBack> _feedBacks;

        private void Awake()
        {
            feedBackEventChannel.OnEventRaised += FeedBack;
        }

        private void Start()
        {
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