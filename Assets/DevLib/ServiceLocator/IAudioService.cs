using DevLib.SoundSystem.Runtime;

namespace DevLib.ServiceLocator
{
    public interface IAudioService
    {
        void PlaySfx(SoundClipSO clipName, int channel = 0);
        void StopSfx(int channel);
        void PlayBgm(SoundClipSO clipName);
        void StopBgm();
    }
}