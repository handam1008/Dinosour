using UnityEngine;

namespace SSW
{
    sealed class SoundVoice
    {
        readonly AudioSource _source;
        float _gain;
        float _start;
        float _end;
        double _duration;
        bool _loop;
        bool _whole;
        public bool Active { get; private set; }
        public double Started { get; private set; }
        public int Channel { get; private set; }

        public SoundVoice(AudioSource source)
        {
            _source = source;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.Stop();
        }

        public void Play(AudioClip clip, float gain, float pitch, float volume, float start, float end, bool loop, int channel)
        {
            Stop();
            _gain = Mathf.Clamp01(gain);
            _start = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - 1f / clip.frequency));
            _end = end > _start ? Mathf.Min(end, clip.length) : clip.length;
            _duration = (_end - _start) / pitch;
            _loop = loop;
            _whole = _start == 0f && _end >= clip.length;
            Channel = channel;
            _source.clip = clip;
            _source.pitch = pitch;
            _source.loop = loop && _whole;
            _source.volume = _gain * volume;
            _source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(_start * clip.frequency), 0, clip.samples - 1);
            _source.Play();
            Started = AudioSettings.dspTime;
            Active = true;
        }

        public void SetVolume(float value) => _source.volume = _gain * value;

        public void Tick()
        {
            if (!Active || _loop && _whole || AudioSettings.dspTime < Started + _duration) return;
            if (!_loop) { Stop(); return; }
            double phase = (AudioSettings.dspTime - Started) % _duration;
            Started = AudioSettings.dspTime - phase;
            _source.time = Mathf.Min(_end, _start + (float)phase * _source.pitch);
            _source.Play();
        }

        public void Stop()
        {
            _source.Stop();
            _source.clip = null;
            Active = false;
            Channel = 0;
        }
    }
}
