using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services  = new Dictionary<Type, object>();

        //얘는 씬이 로드되기전에 무조건 한번 수행됨. 다만 씬이 변경되더라도 다시 수행되지는 않음.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeServiceLocator()
        {
            _services.Clear();
        }
        
        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service; //기존 서비스는 지워진다.
            Debug.Log($"[Service Loader] Register {typeof(T).Name} 등록됨 : {service.GetType().Name}");
        }

        public static void UnRegister<T>(T service)
        {
            _services.Remove(typeof(T));
            Debug.Log($"[Service Loader] UnRegister - {typeof(T).Name}");
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
                return (T)service;
            
            Debug.LogWarning($"[Service Locator] {typeof(T).Name} 이 등록되지 않음");
            return default;
        }
    }
}