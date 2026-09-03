using System;
using UnityEngine;

namespace NKY.Lib.EventChannel
{
    public abstract class EventChannelSO<T> : EventChannelBaseSO
    {
        private static readonly bool IsValueType = typeof(T).IsValueType;
        // 여기 들어갈 것: T를 받는 이벤트/델리게이트 선언 (Action<T>)
        public event Action<T> OnEventRaised;
        
        public void Raise(T value)
        {
            if (!IsValueType && value == null)
            {
                Debug.LogError("Raise event is null");
                return;
            }
                
            // 힌트: 여기도 null 체크
            OnEventRaised?.Invoke(value);
        }
    }
}