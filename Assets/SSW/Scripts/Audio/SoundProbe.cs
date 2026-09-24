#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace SSW
{
    public sealed class SoundProbe : MonoBehaviour
    {
        [Serializable] public sealed class CueState { public string name; public int count; public float at; }
        [Serializable] public sealed class State
        {
            public CueState[] cues;
            public float master;
            public float bgm;
            public float sfx;
            public float peak;
            public float musicPeak;
            public float effectsPeak;
            public int sources;
            public int active;
            public int listeners;
            public int speakers;
        }

        readonly Dictionary<string, CueState> _counts = new Dictionary<string, CueState>();
        readonly float[] _samples = new float[256];
        GameAudio _audio;
        AudioSource[] _sources;
        SoundClipSO _test;
        AudioClip _tone;
        bool _sampling;
        float _peak;
        float _musicPeak;
        float _effectsPeak;
        float _master;
        float _bgm;
        float _sfx;

        void Awake()
        {
            _audio = GameAudio.GetOrCreate();
            _sources = _audio.GetComponentsInChildren<AudioSource>();
            _audio.Played += Heard;
            _master = _audio.MasterVolume;
            _bgm = _audio.BgmVolume;
            _sfx = _audio.SfxVolume;
        }

        void Heard(SoundClipSO cue)
        {
            if (!_counts.TryGetValue(cue.name, out CueState state))
                _counts[cue.name] = state = new CueState { name = cue.name };
            state.count++;
            state.at = Time.unscaledTime;
        }

        public void Execute(int command, float x, float y)
        {
            if (command == 0) { _counts.Clear(); _peak = _musicPeak = _effectsPeak = 0f; _sampling = true; }
            if (command == 1) { _audio.SetMasterVolume(x); _audio.SetBgmVolume(y); }
            if (command == 2) _audio.SetSfxVolume(x);
            if (command == 3)
            {
                if (_tone == null)
                {
                    _tone = AudioClip.Create("Test Tone", 24000, 1, 48000, false);
                    float[] data = new float[24000];
                    for (int i = 0; i < data.Length; i++) data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / 48000f) * 0.05f;
                    _tone.SetData(data, 0);
                    _test = ScriptableObject.CreateInstance<SoundClipSO>();
                    _test.name = "TestMusic"; _test.clip = _tone; _test.isLoop = true;
                    _test.audioType = DevLib.SoundSystem.Runtime.AudioType.Music;
                }
                _audio.PlayBgm(_test);
            }
            if (command == 4) _audio.StopBgm();
            if (command == 5) { _audio.StopSfx(); _peak = _musicPeak = _effectsPeak = 0f; }
            if (command == 6)
            {
                SoundCue cue = _audio.Bank.Cast(PlayerJob.Swordsman, CastKind.Press);
                for (int i = 0; i < 80; i++) _audio.PlaySfx(cue);
            }
            if (command == 7) Restore();
        }

        void Update()
        {
            if (!_sampling) return;
            AudioListener.GetOutputData(_samples, 0);
            _peak = Mathf.Max(_peak, Peak());
            foreach (AudioSource source in _sources)
            {
                if (!source.isPlaying) continue;
                source.GetOutputData(_samples, 0);
                if (source.name == "Bgm") _musicPeak = Mathf.Max(_musicPeak, Peak());
                else _effectsPeak = Mathf.Max(_effectsPeak, Peak());
            }
        }

        float Peak()
        {
            float peak = 0f;
            foreach (float sample in _samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
            return peak;
        }

        public State Read() => new State
        {
            cues = _counts.Values.ToArray(), master = _audio.MasterVolume, bgm = _audio.BgmVolume,
            sfx = _audio.SfxVolume, peak = _peak, musicPeak = _musicPeak, effectsPeak = _effectsPeak,
            sources = _sources.Length, active = _sources.Count(s => s.isPlaying),
            listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled),
            speakers = (int)AudioSettings.speakerMode
        };

        void Restore()
        {
            _sampling = false;
            _audio.StopBgm(); _audio.StopSfx();
            _audio.SetMasterVolume(_master); _audio.SetBgmVolume(_bgm); _audio.SetSfxVolume(_sfx);
        }

        void OnDestroy()
        {
            if (_audio != null) { _audio.Played -= Heard; Restore(); }
            if (_test != null) Destroy(_test);
            if (_tone != null) Destroy(_tone);
        }
    }
}
#endif
