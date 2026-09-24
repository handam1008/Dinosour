using System;
using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEngine;
using AudioType = DevLib.SoundSystem.Runtime.AudioType;

namespace SSW
{
    public sealed class GameAudio : MonoBehaviour, IAudioService
    {
        [SerializeField] SoundBank _bank;
        [SerializeField] AudioSource _music;
        [SerializeField] AudioSource[] _sources;
        readonly System.Random _random = new System.Random();
        SoundVoice _bgm;
        SoundVoice[] _voices;
        SoundClipSO _track;
        public static GameAudio Current { get; private set; }
        public SoundBank Bank => _bank;
        public float MasterVolume { get; private set; }
        public float BgmVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public event Action<SoundClipSO> Played;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() => Current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => GetOrCreate();

        public static GameAudio GetOrCreate()
        {
            if (Current == null) Instantiate(Resources.Load<GameAudio>("Audio/GameAudio"));
            return Current;
        }

        void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this;
            DontDestroyOnLoad(gameObject);
            _bank.Prepare();
            _bgm = new SoundVoice(_music);
            _voices = new SoundVoice[_sources.Length];
            for (int i = 0; i < _sources.Length; i++) _voices[i] = new SoundVoice(_sources[i]);
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BgmVolume", 0.7f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SfxVolume", 1f));
            AudioListener.volume = MasterVolume;
            ServiceLocator.Register<IAudioService>(this);
        }

        void Update()
        {
            _bgm.Tick();
            foreach (SoundVoice voice in _voices) voice.Tick();
        }

        public void Play(SoundClipSO cue)
        {
            if (cue == null) return;
            if (cue.audioType == AudioType.Music) PlayBgm(cue);
            else PlaySfx(cue);
        }

        public void PlaySfx(SoundClipSO cue, int channel = 0)
        {
            if (cue == null || cue.clip == null) return;
            SoundVoice voice = Reserve(channel);
            voice.Play(cue.clip, cue.volume, Pitch(cue), SfxVolume, cue.startTime, cue.endTime, cue.isLoop, channel);
            Played?.Invoke(cue);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            Reserve(0).Play(clip, volume, 1f, SfxVolume, 0f, 0f, false, 0);
        }

        SoundVoice Reserve(int channel)
        {
            if (channel != 0) StopSfx(channel);
            SoundVoice oldest = _voices[0];
            foreach (SoundVoice voice in _voices)
            {
                if (!voice.Active) return voice;
                if (voice.Started < oldest.Started) oldest = voice;
            }
            return oldest;
        }

        float Pitch(SoundClipSO cue) => Mathf.Clamp(cue.pitch + (cue.randomizerPitch ? ((float)_random.NextDouble() * 2f - 1f) * cue.randomPitchModifier : 0f), 0.1f, 3f);

        public void StopSfx(int channel)
        {
            if (channel == 0) return;
            foreach (SoundVoice voice in _voices) if (voice.Channel == channel) voice.Stop();
        }

        public void StopSfx()
        {
            foreach (SoundVoice voice in _voices) voice.Stop();
        }

        public void PlayBgm(SoundClipSO cue)
        {
            if (cue == null || cue.clip == null) { StopBgm(); return; }
            if (_track == cue && _bgm.Active) return;
            _track = cue;
            _bgm.Play(cue.clip, cue.volume, Pitch(cue), BgmVolume, cue.startTime, cue.endTime, cue.isLoop, 0);
            Played?.Invoke(cue);
        }

        public void StopBgm()
        {
            _bgm.Stop();
            _track = null;
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        }

        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            _bgm.SetVolume(BgmVolume);
            PlayerPrefs.SetFloat("BgmVolume", BgmVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            foreach (SoundVoice voice in _voices) voice.SetVolume(SfxVolume);
            PlayerPrefs.SetFloat("SfxVolume", SfxVolume);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) PlayerPrefs.Save();
        }

        void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            if (ReferenceEquals(ServiceLocator.Get<IAudioService>(), this))
                ServiceLocator.Register<IAudioService>(new NullAudioService());
        }
    }
}
