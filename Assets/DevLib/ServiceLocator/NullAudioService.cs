using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    public class NullAudioService : IAudioService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterDefaultService()
        {
            ServiceLocator.Register<IAudioService>(new NullAudioService());
        }
        public void PlaySfx(SoundClipSO clipName, int channel = 0)
        {
        }

        public void StopSfx(int channel)
        {
        }

        public void PlayBgm(SoundClipSO clipName)
        {
        }

        public void StopBgm()
        {
        }
    }
}