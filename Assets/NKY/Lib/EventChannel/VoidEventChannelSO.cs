using System;
using UnityEngine;

namespace NKY.Lib.EventChannel
{
    [CreateAssetMenu(fileName = "VoidEvent", menuName = "Lib/EventChannel/Void", order = 0)]
    public class VoidEventChannelSO : EventChannelBaseSO
    {
        public event Action OnEventRaised;
        
        public void Raise()
        {
            // 힌트: 구독자 없을 때 예외 안 나게 null 체크
            OnEventRaised?.Invoke();
        }
    }
}