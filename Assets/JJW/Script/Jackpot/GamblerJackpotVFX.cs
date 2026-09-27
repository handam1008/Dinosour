using System;
using System.Collections;
using System.Collections.Generic;
using JJW.Script.Jackpot;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GamblerJackpotVFX : MonoBehaviour
{
    private const string DamageArrowPath = "JackpotVFX/DamageArrow";
    private const string SpeedArrowPath = "JackpotVFX/SpeedArrow";
    private const string HealPlusPath = "JackpotVFX/HealPlus";
    private const string DeathMarkPath = "JackpotVFX/DeathMark444";

    [Header("References")]
    [SerializeField] private JackpotDivision division;
    [SerializeField] private DamageUpjackpot damageUpJackpot;
    [SerializeField] private StarJackpot starJackpot;
    [SerializeField] private GameObject invincibleShield;

    [Header("Damage Up - Orange Arrows")]
    [SerializeField, Min(0.01f)] private float damageArrowSize = 0.48f;
    [SerializeField, Min(0f)] private float damageArrowRate = 7f;
    [SerializeField, Min(0f)] private float damageArrowRiseSpeed = 1.2f;

    [Header("Speed Up - Sky Blue Arrows")]
    [SerializeField, Min(0.01f)] private float speedArrowSize = 0.42f;
    [SerializeField, Min(0f)] private float speedArrowRate = 9f;
    [SerializeField, Min(0f)] private float speedArrowRiseSpeed = 1.5f;

    [Header("Heal - Green Plus")]
    [SerializeField, Min(1)] private int healPlusCount = 8;
    [SerializeField, Min(0.01f)] private float healPlusSize = 0.38f;
    [SerializeField, Min(0f)] private float healPlusRiseSpeed = 1.4f;

    [Header("Rendering")]
    [SerializeField] private int particleSortingOrder = 20;

    [Header("444 - Death Mark")]
    [SerializeField, Min(1f)] private float deathMarkSize = 760f;
    [SerializeField, Min(0.1f)] private float deathMarkDisplayScale = 1.3f;
    [SerializeField, Range(0f, 1f)] private float deathMarkMaxAlpha = 0.65f;
    [SerializeField, Min(0.01f)] private float deathMarkFadeIn = 0.08f;
    [SerializeField, Min(0f)] private float deathMarkHold = 0.3f;
    [SerializeField, Min(0.01f)] private float deathMarkFadeOut = 0.4f;

    [Header("444 Sound - Add Later")]
    [SerializeField] private AudioSource instantKillAudioSource;
    [SerializeField] private AudioClip instantKillSound;

    private readonly List<Material> runtimeMaterials = new List<Material>();

    private ParticleSystem damageParticles;
    private ParticleSystem speedParticles;
    private ParticleSystem healParticles;

    private Coroutine speedEffectCoroutine;
    private float speedEffectEndTime;

    private CanvasGroup deathMarkCanvasGroup;
    private RectTransform deathMarkTransform;
    private Sprite deathMarkSprite;
    private Coroutine deathMarkCoroutine;

    private void Awake()
    {
        FindReferences();
        CreateParticleSystems();
        CreateDeathMarkUI();
        SetShieldActive(false);
    }

    private void OnEnable()
    {
        if (damageUpJackpot != null)
        {
            damageUpJackpot.DamageStackAdded += HandleDamageStackAdded;
            damageUpJackpot.DamageBuffEnded += StopDamageEffect;

            if (damageUpJackpot.IsActive)
            {
                StartLoopingEffect(damageParticles);
            }
        }

        if (division != null)
        {
            division.SpeedJackpot += HandleSpeedJackpot;
            division.HealJackpot += HandleHealJackpot;
            division.Jackpot444 += HandleInstantKillJackpot;
        }

        if (starJackpot != null)
        {
            starJackpot.InvincibilityStarted += HandleInvincibilityStarted;
            starJackpot.InvincibilityEnded += HandleInvincibilityEnded;

            if (starJackpot.IsInvincible)
            {
                SetShieldActive(true);
            }
        }
    }

    private void OnDisable()
    {
        if (damageUpJackpot != null)
        {
            damageUpJackpot.DamageStackAdded -= HandleDamageStackAdded;
            damageUpJackpot.DamageBuffEnded -= StopDamageEffect;
        }

        if (division != null)
        {
            division.SpeedJackpot -= HandleSpeedJackpot;
            division.HealJackpot -= HandleHealJackpot;
            division.Jackpot444 -= HandleInstantKillJackpot;
        }

        if (starJackpot != null)
        {
            starJackpot.InvincibilityStarted -= HandleInvincibilityStarted;
            starJackpot.InvincibilityEnded -= HandleInvincibilityEnded;
        }

        StopAllEffects();
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        runtimeMaterials.Clear();

        if (deathMarkSprite != null)
        {
            Destroy(deathMarkSprite);
        }
    }

    private void FindReferences()
    {
        if (division == null)
        {
            division = GetComponent<JackpotDivision>();
        }

        if (damageUpJackpot == null)
        {
            damageUpJackpot = GetComponent<DamageUpjackpot>();
        }

        if (starJackpot == null)
        {
            starJackpot = GetComponent<StarJackpot>();
        }

        if (invincibleShield == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == "InvincibleShieldEffect")
                {
                    invincibleShield = child.gameObject;
                    break;
                }
            }
        }
    }

    private void CreateParticleSystems()
    {
        damageParticles = CreateRisingEffect(
            "DamageUpVFX",
            DamageArrowPath,
            damageArrowSize,
            damageArrowRate,
            damageArrowRiseSpeed,
            new Vector3(0f, -0.35f, 0f));

        speedParticles = CreateRisingEffect(
            "SpeedUpVFX",
            SpeedArrowPath,
            speedArrowSize,
            speedArrowRate,
            speedArrowRiseSpeed,
            new Vector3(0f, -0.35f, 0f));

        healParticles = CreateHealEffect();
    }

    private ParticleSystem CreateRisingEffect(string objectName, string resourcePath, float size, float emissionRate,
        float riseSpeed,
        Vector3 localPosition)
    {
        ParticleSystem particle = CreateBaseParticleSystem(objectName, resourcePath, localPosition);

        if (particle == null)
        {
            return null;
        }

        ParticleSystem.MainModule main = particle.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = 0.85f;
        main.startSpeed = 0f;
        main.startSize = size;
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.9f, 0.08f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity =
            particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        velocity.y = new ParticleSystem.MinMaxCurve(
            riseSpeed * 0.95f,
            riseSpeed * 1.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        SetFadeOverLifetime(particle);
        return particle;
    }

    private ParticleSystem CreateHealEffect()
    {
        ParticleSystem particle = CreateBaseParticleSystem("HealVFX", HealPlusPath, new Vector3(0f, -0.15f, 0f));

        if (particle == null)
        {
            return null;
        }

        ParticleSystem.MainModule main = particle.main;
        main.loop = false;
        main.duration = 0.7f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(healPlusSize * 0.8f, healPlusSize * 1.2f);
        main.maxParticles = 20;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)healPlusCount) });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.8f, 0.15f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(healPlusRiseSpeed * 0.85f, healPlusRiseSpeed * 1.15f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        SetFadeOverLifetime(particle);
        return particle;
    }

    private ParticleSystem CreateBaseParticleSystem(string objectName, string resourcePath, Vector3 localPosition)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);

        if (texture == null)
        {
            Debug.LogWarning($"파티클 텍스처를 찾지 못했습니다: Resources/{resourcePath}", this);
            return null;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("Sprites/Default 셰이더를 찾지 못했습니다.", this);
            return null;
        }

        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = localPosition;

        ParticleSystem particle = effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Material material = new Material(shader)
        {
            name = $"{objectName}_RuntimeMaterial",
            mainTexture = texture
        };

        runtimeMaterials.Add(material);

        ParticleSystemRenderer particleRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = ParticleSystemRenderSpace.View;
        particleRenderer.material = material;
        particleRenderer.sortingOrder = particleSortingOrder;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        return particle;
    }

    private void CreateDeathMarkUI()
    {
        Texture2D texture = Resources.Load<Texture2D>(DeathMarkPath);

        if (texture == null)
        {
            Debug.LogWarning(
                $"444 이미지를 찾지 못했습니다: Resources/{DeathMarkPath}",
                this);
            return;
        }

        deathMarkSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        GameObject canvasObject = new GameObject(
            "DeathMark444Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject(
            "DeathMark444",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        imageObject.transform.SetParent(canvasObject.transform, false);

        deathMarkTransform = imageObject.GetComponent<RectTransform>();
        deathMarkTransform.anchorMin = new Vector2(0.5f, 0.5f);
        deathMarkTransform.anchorMax = new Vector2(0.5f, 0.5f);
        deathMarkTransform.pivot = new Vector2(0.5f, 0.5f);
        deathMarkTransform.anchoredPosition = Vector2.zero;
        deathMarkTransform.sizeDelta = Vector2.one * deathMarkSize;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = deathMarkSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        deathMarkCanvasGroup = imageObject.GetComponent<CanvasGroup>();
        deathMarkCanvasGroup.alpha = 0f;
        deathMarkCanvasGroup.interactable = false;
        deathMarkCanvasGroup.blocksRaycasts = false;
    }

    private static void SetFadeOverLifetime(ParticleSystem particle)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.85f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule color = particle.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    private void HandleDamageStackAdded(bool _)
    {
        StartLoopingEffect(damageParticles);
    }

    private void StopDamageEffect()
    {
        StopEffect(damageParticles);
    }

    private void HandleSpeedJackpot(float _, float duration)
    {
        StartLoopingEffect(speedParticles);

        speedEffectEndTime = Mathf.Max(speedEffectEndTime, Time.time + Mathf.Max(0f, duration));

        if (speedEffectCoroutine == null)
        {
            speedEffectCoroutine = StartCoroutine(StopSpeedEffectAfterTime());
        }
    }

    private IEnumerator StopSpeedEffectAfterTime()
    {
        while (Time.time < speedEffectEndTime)
        {
            yield return null;
        }

        StopEffect(speedParticles);
        speedEffectCoroutine = null;
    }

    private void HandleHealJackpot(float _)
    {
        if (healParticles == null)
        {
            return;
        }

        healParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        healParticles.Play(true);
    }

    private void HandleInvincibilityStarted(float _)
    {
        SetShieldActive(true);
    }

    private void HandleInvincibilityEnded()
    {
        SetShieldActive(false);
    }

    private void HandleInstantKillJackpot(float _)
    {
        if (deathMarkCanvasGroup == null)
        {
            return;
        }

        if (deathMarkCoroutine != null)
        {
            StopCoroutine(deathMarkCoroutine);
        }

        if (instantKillAudioSource != null && instantKillSound != null)
        {
            instantKillAudioSource.PlayOneShot(instantKillSound);
        }

        deathMarkCoroutine = StartCoroutine(PlayDeathMark());
    }

    private IEnumerator PlayDeathMark()
    {
        deathMarkCanvasGroup.alpha = 0f;
        deathMarkTransform.localScale = Vector3.one
            * (1.55f * deathMarkDisplayScale);

        float elapsed = 0f;

        while (elapsed < deathMarkFadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / deathMarkFadeIn);

            deathMarkCanvasGroup.alpha = progress * deathMarkMaxAlpha;
            deathMarkTransform.localScale = Vector3.one
                * Mathf.Lerp(
                    1.55f * deathMarkDisplayScale,
                    deathMarkDisplayScale,
                    progress);

            yield return null;
        }

        deathMarkCanvasGroup.alpha = deathMarkMaxAlpha;
        deathMarkTransform.localScale = Vector3.one
            * deathMarkDisplayScale;

        if (deathMarkHold > 0f)
        {
            yield return new WaitForSecondsRealtime(deathMarkHold);
        }

        elapsed = 0f;

        while (elapsed < deathMarkFadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / deathMarkFadeOut);

            deathMarkCanvasGroup.alpha =
                deathMarkMaxAlpha * (1f - progress);
            deathMarkTransform.localScale = Vector3.one
                * Mathf.Lerp(
                    deathMarkDisplayScale,
                    deathMarkDisplayScale * 1.12f,
                    progress);

            yield return null;
        }

        HideDeathMark();
        deathMarkCoroutine = null;
    }

    private void HideDeathMark()
    {
        if (deathMarkCanvasGroup != null)
        {
            deathMarkCanvasGroup.alpha = 0f;
        }

        if (deathMarkTransform != null)
        {
            deathMarkTransform.localScale = Vector3.one
                * deathMarkDisplayScale;
        }
    }

    private static void StartLoopingEffect(ParticleSystem particle)
    {
        if (particle == null || particle.isPlaying)
        {
            return;
        }

        particle.Play(true);
    }

    private static void StopEffect(ParticleSystem particle)
    {
        if (particle == null)
        {
            return;
        }

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetShieldActive(bool active)
    {
        if (invincibleShield != null && invincibleShield != gameObject)
        {
            invincibleShield.SetActive(active);
        }
    }

    private void StopAllEffects()
    {
        if (speedEffectCoroutine != null)
        {
            StopCoroutine(speedEffectCoroutine);
            speedEffectCoroutine = null;
        }

        speedEffectEndTime = 0f;

        if (deathMarkCoroutine != null)
        {
            StopCoroutine(deathMarkCoroutine);
            deathMarkCoroutine = null;
        }

        HideDeathMark();
        StopEffect(damageParticles);
        StopEffect(speedParticles);
        StopEffect(healParticles);
        SetShieldActive(false);
    }
}
