using UnityEngine;

namespace SSW
{
    public sealed class SoundEmitter : MonoBehaviour
    {
        [SerializeField] SoundCue _cue;
        [SerializeField] bool _shared;
        [SerializeField] bool _playOnEnable;
        [SerializeField, Min(0)] int _channel;

        static int _next;
        int _handle;
        int Channel
        {
            get
            {
                if (_channel != 0) return _channel;
                if (_handle == 0) _handle = --_next;
                return _handle;
            }
        }

        void OnEnable()
        {
            if (_playOnEnable) Play();
        }

        public void Play()
        {
            GameAudio audio = GameAudio.GetOrCreate();
            if (_shared && NetGame.Current != null && NetGame.Current.Connected)
                NetGame.Current.Sounds.Play(_cue);
            else if (_cue != null && _cue.audioType == DevLib.SoundSystem.Runtime.AudioType.Music)
                audio.PlayBgm(_cue);
            else audio.PlaySfx(_cue, Channel);
        }

        public void Stop()
        {
            GameAudio audio = GameAudio.GetOrCreate();
            if (_cue != null && _cue.audioType == DevLib.SoundSystem.Runtime.AudioType.Music) audio.StopBgm();
            else audio.StopSfx(Channel);
        }
    }
}
