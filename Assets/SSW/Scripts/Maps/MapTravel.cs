using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSW
{
    public static class MapTravel
    {
        static MapLayout _next;

        public static bool IsLocal => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            _next = null;
        }

        public static void Open(MapLayout map)
        {
            if (!IsLocal) return;
            _next = map;
            Time.timeScale = 1f;
            SceneManager.LoadScene("SuperUltraLegendScene");
        }

        public static MapLayout Take()
        {
            MapLayout map = _next;
            _next = null;
            return map;
        }
    }
}
