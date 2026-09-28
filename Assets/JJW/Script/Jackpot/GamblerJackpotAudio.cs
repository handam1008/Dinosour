using SSW;
using UnityEngine;

namespace JJW.Script.Jackpot
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class GamblerJackpotAudio : MonoBehaviour
    {
        private const string RouletteVoicePath =
            "GamblerAudio/GameboyJones777Roulette";

        private const string JackpotMusicPath =
            "GamblerAudio/GameboyJones777Active";

        private const float NetworkJackpotDuration = 15f;

        [Header("777 Audio")]
        [SerializeField, Range(0f, 1f)]
        private float rouletteVoiceVolume = 1f;

        [SerializeField, Min(0.01f)]
        private float rouletteVoiceFadeOutDuration = 0.25f;

        [SerializeField, Range(0f, 1f)]
        private float jackpotMusicVolume = 0.8f;

        [SerializeField, Min(0.01f)]
        private float fadeOutDuration = 2f;

        private JackpotDivision division;
        private global::Jackpot777 jackpotState;
        private CoinFx networkEffects;

        private AudioSource rouletteVoiceSource;
        private AudioSource jackpotMusicSource;
        private AudioClip rouletteVoiceClip;
        private AudioClip jackpotMusicClip;

        private int observedNetworkRolls;
        private bool observedNetworkJackpot;

        private bool rouletteVoiceActive;
        private float rouletteVoiceEndTime;
        private float rouletteVoiceFadeStartTime;

        private bool musicActive;
        private bool forcedFade;
        private float musicEndTime;
        private float fadeStartTime;
        private float fadeStartVolume;

        private void Awake()
        {
            division = GetComponent<JackpotDivision>();
            jackpotState = GetComponentInParent<global::Jackpot777>();
            networkEffects = GetComponent<CoinFx>();

            rouletteVoiceClip =
                Resources.Load<AudioClip>(RouletteVoicePath);

            jackpotMusicClip =
                Resources.Load<AudioClip>(JackpotMusicPath);

            rouletteVoiceSource = CreateAudioSource("777 Roulette Voice");
            jackpotMusicSource = CreateAudioSource("777 Jackpot Music");

            if (networkEffects != null)
            {
                observedNetworkRolls = networkEffects.Rolls;
                observedNetworkJackpot = networkEffects.Jackpot;
            }
        }

        private void OnEnable()
        {
            if (division != null)
            {
                division.Jackpot777RouletteStarted +=
                    PlayRouletteVoice;
            }

            if (jackpotState != null)
            {
                jackpotState.JackpotStarted += PlayJackpotMusic;
                jackpotState.JackpotEnded += FadeOutJackpotMusic;
            }
        }

        private void OnDisable()
        {
            if (division != null)
            {
                division.Jackpot777RouletteStarted -=
                    PlayRouletteVoice;
            }

            if (jackpotState != null)
            {
                jackpotState.JackpotStarted -= PlayJackpotMusic;
                jackpotState.JackpotEnded -= FadeOutJackpotMusic;
            }

            StopAllAudio();
        }

        private void LateUpdate()
        {
            PollNetworkEffects();
            UpdateRouletteVoiceFade();
            UpdateMusicFade();
        }

        private void PollNetworkEffects()
        {
            if (networkEffects == null)
            {
                return;
            }

            if (networkEffects.Rolls != observedNetworkRolls)
            {
                observedNetworkRolls = networkEffects.Rolls;

                if (networkEffects.Main == JackpotResultType.Jackpot777
                    || networkEffects.Old == JackpotResultType.Jackpot777)
                {
                    PlayRouletteVoice();
                }
            }

            bool jackpotActive = networkEffects.Jackpot;

            if (jackpotActive && !observedNetworkJackpot)
            {
                PlayJackpotMusic(NetworkJackpotDuration);
            }
            else if (!jackpotActive && observedNetworkJackpot)
            {
                FadeOutJackpotMusic();
            }

            observedNetworkJackpot = jackpotActive;
        }

        private AudioSource CreateAudioSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private void PlayRouletteVoice()
        {
            if (rouletteVoiceClip == null || rouletteVoiceSource == null)
            {
                return;
            }

            rouletteVoiceSource.Stop();
            rouletteVoiceSource.clip = rouletteVoiceClip;
            rouletteVoiceSource.volume = rouletteVoiceVolume;
            rouletteVoiceSource.Play();

            rouletteVoiceActive = true;
            rouletteVoiceEndTime = Time.time
                                   + rouletteVoiceClip.length;
            rouletteVoiceFadeStartTime = rouletteVoiceEndTime
                - Mathf.Min(
                    rouletteVoiceFadeOutDuration,
                    rouletteVoiceClip.length);
        }

        private void UpdateRouletteVoiceFade()
        {
            if (!rouletteVoiceActive || rouletteVoiceSource == null)
            {
                return;
            }

            if (Time.time >= rouletteVoiceEndTime
                || !rouletteVoiceSource.isPlaying)
            {
                StopRouletteVoice();
                return;
            }

            if (Time.time < rouletteVoiceFadeStartTime)
            {
                return;
            }

            float fadeLength = Mathf.Max(
                0.01f,
                rouletteVoiceEndTime - rouletteVoiceFadeStartTime);

            float progress = Mathf.Clamp01(
                (Time.time - rouletteVoiceFadeStartTime) / fadeLength);

            rouletteVoiceSource.volume =
                Mathf.Lerp(rouletteVoiceVolume, 0f, progress);
        }

        private void StopRouletteVoice()
        {
            if (rouletteVoiceSource != null)
            {
                rouletteVoiceSource.Stop();
                rouletteVoiceSource.clip = null;
                rouletteVoiceSource.volume = rouletteVoiceVolume;
            }

            rouletteVoiceActive = false;
        }

        private void PlayJackpotMusic(float duration)
        {
            if (jackpotMusicClip == null || jackpotMusicSource == null)
            {
                return;
            }

            jackpotMusicSource.Stop();
            jackpotMusicSource.clip = jackpotMusicClip;
            jackpotMusicSource.volume = jackpotMusicVolume;
            jackpotMusicSource.time = 0f;
            jackpotMusicSource.Play();

            float playableDuration = Mathf.Min(
                Mathf.Max(0.01f, duration),
                jackpotMusicClip.length);

            musicActive = true;
            forcedFade = false;
            musicEndTime = Time.time + playableDuration;
            fadeStartTime = musicEndTime
                            - Mathf.Min(fadeOutDuration, playableDuration);
            fadeStartVolume = jackpotMusicVolume;
        }

        private void FadeOutJackpotMusic()
        {
            if (!musicActive || jackpotMusicSource == null)
            {
                return;
            }

            if (!forcedFade && Time.time >= fadeStartTime)
            {
                return;
            }

            forcedFade = true;
            fadeStartTime = Time.time;
            musicEndTime = Time.time + fadeOutDuration;
            fadeStartVolume = jackpotMusicSource.volume;
        }

        private void UpdateMusicFade()
        {
            if (!musicActive || jackpotMusicSource == null)
            {
                return;
            }

            if (Time.time >= musicEndTime
                || !jackpotMusicSource.isPlaying)
            {
                StopJackpotMusic();
                return;
            }

            if (Time.time < fadeStartTime)
            {
                return;
            }

            float fadeLength = Mathf.Max(
                0.01f,
                musicEndTime - fadeStartTime);

            float progress = Mathf.Clamp01(
                (Time.time - fadeStartTime) / fadeLength);

            float startVolume = forcedFade
                ? fadeStartVolume
                : jackpotMusicVolume;

            jackpotMusicSource.volume =
                Mathf.Lerp(startVolume, 0f, progress);
        }

        private void StopJackpotMusic()
        {
            if (jackpotMusicSource != null)
            {
                jackpotMusicSource.Stop();
                jackpotMusicSource.clip = null;
                jackpotMusicSource.volume = jackpotMusicVolume;
            }

            musicActive = false;
            forcedFade = false;
        }

        private void StopAllAudio()
        {
            StopRouletteVoice();
            StopJackpotMusic();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallNetworkBootstrap()
        {
            if (Object.FindAnyObjectByType<
                    GamblerJackpotAudioBootstrap>(
                    FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject bootstrapObject =
                new GameObject("Gambler Jackpot Audio Bootstrap");

            Object.DontDestroyOnLoad(bootstrapObject);
            bootstrapObject.AddComponent<
                GamblerJackpotAudioBootstrap>();
        }
    }

    internal sealed class GamblerJackpotAudioBootstrap : MonoBehaviour
    {
        private float nextSearchTime;

        private void Update()
        {
            if (Time.unscaledTime < nextSearchTime)
            {
                return;
            }

            nextSearchTime = Time.unscaledTime + 0.25f;

            CoinFx[] networkEffects =
                Object.FindObjectsByType<CoinFx>(
                    FindObjectsInactive.Include);

            foreach (CoinFx effect in networkEffects)
            {
                if (effect.GetComponent<GamblerJackpotAudio>() == null)
                {
                    effect.gameObject.AddComponent<
                        GamblerJackpotAudio>();
                }
            }
        }
    }
}
