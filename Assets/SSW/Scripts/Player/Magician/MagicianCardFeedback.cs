using System;
using UnityEngine;

namespace SSW
{
    [Serializable]
    public sealed class MagicianCardFeedbackSettings
    {
        public bool enabled = true;

        [Header("Suit Colors")]
        public Color spadeColor = new Color(0.58f, 0.3f, 1f, 1f);
        public Color heartColor = new Color(1f, 0.2f, 0.38f, 1f);
        public Color diamondColor = new Color(0.18f, 0.85f, 1f, 1f);
        public Color cloverColor = new Color(0.28f, 1f, 0.42f, 1f);
        public Color jokerColor = new Color(1f, 0.78f, 0.2f, 1f);

        [Header("Trail")]
        [Min(0.02f)] public float trailTime = 0.16f;
        [Min(0.01f)] public float trailWidth = 0.12f;

        [Header("Impact")]
        [Min(1)] public int minimumParticles = 8;
        [Min(1)] public int maximumParticles = 18;
        [Min(0.01f)] public float flashDuration = 0.075f;
        [Min(0f)] public float minimumShake = 0.035f;
        [Min(0f)] public float maximumShake = 0.1f;
        [Min(0.01f)] public float shakeDuration = 0.13f;

        [Header("Optional Audio")]
        [Range(0f, 1f)] public float volume = 0.75f;
        public AudioClip launchClip;
        public AudioClip defaultImpactClip;
        public AudioClip spadeImpactClip;
        public AudioClip heartImpactClip;
        public AudioClip diamondImpactClip;
        public AudioClip cloverImpactClip;

        public Color ColorFor(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spade: return spadeColor;
                case Suit.Heart: return heartColor;
                case Suit.Diamond: return diamondColor;
                default: return cloverColor;
            }
        }

        public AudioClip ImpactClipFor(Suit suit)
        {
            AudioClip clip;
            switch (suit)
            {
                case Suit.Spade: clip = spadeImpactClip; break;
                case Suit.Heart: clip = heartImpactClip; break;
                case Suit.Diamond: clip = diamondImpactClip; break;
                default: clip = cloverImpactClip; break;
            }

            return clip != null ? clip : defaultImpactClip;
        }
    }

    /// <summary>Owns visual and audio feedback for one runtime-created magician card.</summary>
    [DisallowMultipleComponent]
    public sealed class MagicianCardFeedback : MonoBehaviour
    {
        MagicianCardFeedbackSettings _settings;
        Suit _suit;
        int _number;
        float _intensityMultiplier = 1f;
        bool _isJoker;
        GameObject _trailObject;
        TrailRenderer _trail;
        GameObject _coreTrailObject;
        TrailRenderer _coreTrail;

        float Rank01 => Mathf.InverseLerp(1f, 10f, Mathf.Clamp(_number, 1, 10));
        float Intensity => Mathf.Lerp(0.75f, 1.35f, Rank01) * _intensityMultiplier;
        Color EffectColor => _isJoker ? _settings.jokerColor : _settings.ColorFor(_suit);

        public void Configure(
            MagicianCardFeedbackSettings settings,
            Suit suit,
            int number,
            Material trailMaterial)
        {
            _settings = settings;
            _suit = suit;
            _number = number;

            if (_settings == null || !_settings.enabled) return;
            MagicianBloomRuntime.Ensure();
            CreateTrail(trailMaterial);
        }

        public void SetIntensityMultiplier(float multiplier)
        {
            _intensityMultiplier = Mathf.Max(0f, multiplier);
            UpdateTrailStyle();
        }

        public void MarkJoker()
        {
            _isJoker = true;
            UpdateTrailStyle();
        }

        public void PlayLaunch()
        {
            if (_settings == null || !_settings.enabled) return;
            MagicianImpactVfx.SpawnLaunch(
                transform.position,
                _suit,
                EffectColor,
                Mathf.Max(0.1f, Intensity),
                _isJoker);
            PlayClip(_settings.launchClip, transform.position, _settings.volume * Mathf.Clamp01(Intensity));
        }

        public void PlayTargetFlash(Component target)
        {
            if (_settings == null || !_settings.enabled || target == null) return;

            Color flashColor = Color.Lerp(EffectColor, Color.white, 0.55f);
            HitFlashFeedback.Play(target, flashColor, _settings.flashDuration);
        }

        public void PlayImpact(Vector3 position, float effectRadius = 0f)
        {
            if (_settings == null || !_settings.enabled) return;

            float intensity = Mathf.Max(0.1f, Intensity);
            MagicianImpactVfx.SpawnImpact(
                position,
                _suit,
                EffectColor,
                intensity,
                _isJoker,
                effectRadius);

            float shake = Mathf.Lerp(_settings.minimumShake, _settings.maximumShake, Rank01) * intensity;
            CameraShakeFeedback.Play(shake, _settings.shakeDuration);

            AudioClip clip = _settings.ImpactClipFor(_suit);
            PlayClip(clip, position, _settings.volume * Mathf.Clamp01(intensity));
        }

        public void PlayEnvironmentImpact(Vector3 position)
        {
            if (_settings == null || !_settings.enabled) return;

            float intensity = Mathf.Max(0.1f, Intensity * 0.72f);
            MagicianImpactVfx.SpawnSurfaceImpact(
                position,
                _suit,
                EffectColor,
                intensity,
                _isJoker);

            AudioClip clip = _settings.ImpactClipFor(_suit);
            PlayClip(clip, position, _settings.volume * 0.35f * Mathf.Clamp01(intensity));
        }

        public void ReleaseTrail()
        {
            ReleaseTrailRenderer(ref _trailObject, ref _trail);
            ReleaseTrailRenderer(ref _coreTrailObject, ref _coreTrail);
        }

        void OnDisable()
        {
            ReleaseTrail();
        }

        void CreateTrail(Material trailMaterial)
        {
            _trailObject = new GameObject("CardTrail");
            _trailObject.transform.SetParent(transform, false);
            _trail = CreateTrailRenderer(
                _trailObject,
                MagicianVfxMaterials.Get(MagicianGlowShape.Stripe, 2.8f),
                trailMaterial,
                13);

            _coreTrailObject = new GameObject("CardTrailCore");
            _coreTrailObject.transform.SetParent(transform, false);
            _coreTrail = CreateTrailRenderer(
                _coreTrailObject,
                MagicianVfxMaterials.Get(MagicianGlowShape.Stripe, 4.8f),
                trailMaterial,
                14);
            _coreTrail.minVertexDistance = 0.02f;

            UpdateTrailStyle();
        }

        void UpdateTrailStyle()
        {
            if (_settings == null) return;

            Color color = EffectColor;
            Color outerStart = color;
            outerStart.a = 0.8f;
            Color outerEnd = color;
            outerEnd.a = 0f;

            if (_trail != null)
            {
                _trail.startColor = outerStart;
                _trail.endColor = outerEnd;
                _trail.widthMultiplier = _settings.trailWidth * 1.55f * Mathf.Max(0.2f, Intensity);
            }

            if (_coreTrail != null)
            {
                Color coreStart = Color.Lerp(color, Color.white, 0.82f);
                coreStart.a = 0.95f;
                Color coreEnd = coreStart;
                coreEnd.a = 0f;
                _coreTrail.startColor = coreStart;
                _coreTrail.endColor = coreEnd;
                _coreTrail.widthMultiplier = _settings.trailWidth * 0.42f * Mathf.Max(0.2f, Intensity);
            }
        }

        TrailRenderer CreateTrailRenderer(
            GameObject trailObject,
            Material glowMaterial,
            Material fallbackMaterial,
            int sortingOrder)
        {
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = _settings.trailTime * 1.15f;
            trail.minVertexDistance = 0.035f;
            trail.numCornerVertices = 4;
            trail.numCapVertices = 4;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.sortingOrder = sortingOrder;
            trail.sharedMaterial = glowMaterial != null ? glowMaterial : fallbackMaterial;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.72f, 0.72f),
                new Keyframe(1f, 0f));
            return trail;
        }

        void ReleaseTrailRenderer(ref GameObject trailObject, ref TrailRenderer trail)
        {
            if (trailObject == null) return;

            trailObject.transform.SetParent(null, true);
            if (trail != null)
            {
                trail.emitting = false;
                Destroy(trailObject, Mathf.Max(0.05f, trail.time + 0.05f));
            }
            else
            {
                Destroy(trailObject);
            }

            trailObject = null;
            trail = null;
        }

        static void PlayClip(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null || volume <= 0f) return;

            GameObject audioObject = new GameObject("MagicianCardAudio");
            audioObject.transform.position = position;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = Mathf.Clamp01(volume);
            source.clip = clip;
            source.Play();

            Destroy(audioObject, clip.length + 0.1f);
        }
    }
}
