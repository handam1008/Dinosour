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
        Material _effectMaterial;
        GameObject _trailObject;
        TrailRenderer _trail;

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
            _effectMaterial = trailMaterial;

            if (_settings == null || !_settings.enabled) return;
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
            PlayClip(_settings.launchClip, transform.position, _settings.volume * Mathf.Clamp01(Intensity));
        }

        public void PlayTargetFlash(Component target)
        {
            if (_settings == null || !_settings.enabled || target == null) return;

            Color flashColor = Color.Lerp(EffectColor, Color.white, 0.55f);
            HitFlashFeedback.Play(target, flashColor, _settings.flashDuration);
        }

        public void PlayImpact(Vector3 position)
        {
            if (_settings == null || !_settings.enabled) return;

            float intensity = Mathf.Max(0.1f, Intensity);
            SpawnImpactParticles(position, EffectColor, intensity);

            float shake = Mathf.Lerp(_settings.minimumShake, _settings.maximumShake, Rank01) * intensity;
            CameraShakeFeedback.Play(shake, _settings.shakeDuration);

            AudioClip clip = _settings.ImpactClipFor(_suit);
            PlayClip(clip, position, _settings.volume * Mathf.Clamp01(intensity));
        }

        public void ReleaseTrail()
        {
            if (_trailObject == null) return;

            _trailObject.transform.SetParent(null, true);
            if (_trail != null)
            {
                _trail.emitting = false;
                Destroy(_trailObject, Mathf.Max(0.05f, _trail.time + 0.05f));
            }
            else
            {
                Destroy(_trailObject);
            }

            _trailObject = null;
            _trail = null;
        }

        void OnDisable()
        {
            ReleaseTrail();
        }

        void CreateTrail(Material trailMaterial)
        {
            _trailObject = new GameObject("CardTrail");
            _trailObject.transform.SetParent(transform, false);

            _trail = _trailObject.AddComponent<TrailRenderer>();
            _trail.time = _settings.trailTime;
            _trail.minVertexDistance = 0.035f;
            _trail.numCornerVertices = 2;
            _trail.numCapVertices = 2;
            _trail.alignment = LineAlignment.View;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.sortingOrder = 14;
            if (trailMaterial != null)
                _trail.sharedMaterial = trailMaterial;

            _trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f));
            UpdateTrailStyle();
        }

        void UpdateTrailStyle()
        {
            if (_trail == null || _settings == null) return;

            Color color = EffectColor;
            Color transparent = color;
            transparent.a = 0f;
            _trail.startColor = color;
            _trail.endColor = transparent;
            _trail.widthMultiplier = _settings.trailWidth * Mathf.Max(0.2f, Intensity);
        }

        void SpawnImpactParticles(Vector3 position, Color color, float intensity)
        {
            GameObject effect = new GameObject("MagicianCardImpact");
            effect.transform.position = position;

            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.28f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f * intensity, 3.2f * intensity);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * intensity, 0.13f * intensity);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.maxParticles = Mathf.Max(32, _settings.maximumParticles * 2);
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f * intensity;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.25f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = fade;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(1f, 0f)));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 20;
            if (_effectMaterial != null)
                renderer.sharedMaterial = _effectMaterial;

            int count = Mathf.RoundToInt(
                Mathf.Lerp(_settings.minimumParticles, _settings.maximumParticles, Rank01) * intensity);
            count = Mathf.Clamp(count, 2, 64);

            particles.Play();
            particles.Emit(count);
            Destroy(effect, 1f);
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
