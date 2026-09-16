using System;
using UnityEngine;

namespace SSW
{
    public static class NetLaunch
    {
        public static string Name => Read("--net-name", "");
        public static string Tag => Read("--net-title", "");
        public static string Profile => Read("--net-profile", "default");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.runInBackground = true;
            string mode = Read("--net-mode", "");
            if (mode == "host" || mode == "client")
            {
                PlayerJob job = Read("--net-job", "Magician") == "Witch" ? PlayerJob.Witch : PlayerJob.Magician;
                ushort port = ushort.TryParse(Read("--net-port", "7777"), out ushort parsed) ? parsed : (ushort)7777;
                NetGame.GetOrCreate().StartLocal(mode == "host", Read("--net-address", "127.0.0.1"), job, port);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string probe = Read("--net-probe", "");
            if (probe.Length > 0)
            {
                NetGame game = NetGame.GetOrCreate();
                game.gameObject.AddComponent<NetProbe>().Init(probe);
                game.gameObject.AddComponent<MenuProbe>().Init(probe);
            }
#endif
        }

        static string Read(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
    }
}
