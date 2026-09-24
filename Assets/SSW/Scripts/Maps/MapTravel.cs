using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSW
{
    public static class MapTravel
    {
        static MapLayout _next;

        public static bool IsLocal => NetGame.Current != null && NetGame.Current.Practice != null
            || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            _next = null;
        }

        public static void Open(MapLayout map)
        {
            if (!IsLocal) return;
            _next = map;
            LoadScene("SuperUltraLegendScene");
        }

        public static void LoadScene(string scene)
        {
            if (!IsLocal) return;
            Time.timeScale = 1f;
            if (NetGame.Current != null && NetGame.Current.Practice != null) NetGame.Current.OpenPracticeScene(scene);
            else SceneManager.LoadScene(scene);
        }

        public static MapLayout Take()
        {
            MapLayout map = _next;
            _next = null;
            return map;
        }
    }
}
