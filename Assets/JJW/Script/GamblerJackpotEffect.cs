using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GamblerJackpotEffect : MonoBehaviour
{
    [Header("Jackpot Event")]
    [SerializeField] private Jackpot777 jackpot777;

    [Header("777 UI")]
    [SerializeField] private JackpotUI jackpotUI;
    [SerializeField] private CameraShake cameraShake;

    [Header("Overlay")]
    [SerializeField] private Image jackpotOverlay;
    [SerializeField, Range(0f, 1f)] private float minOverlayAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 0.45f;
    [SerializeField, Min(0.01f)] private float overlayDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float overlayFadeOutDuration = 0.15f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem[] jackpotParticles;

    [Header("Active During Jackpot")]
    [SerializeField] private GameObject[] jackpotObjects;

    private Sequence overlaySequence;

    private void Awake()
    {
        StopAllEffectsImmediately();
    }

    private void OnEnable()
    {
        if (jackpot777 != null)
        {
            jackpot777.JackpotStarted += HandleJackpotStarted;
            jackpot777.JackpotEnded += HandleJackpotEnded;
        }

        if (jackpotUI != null)
        {
            jackpotUI.Impacted += HandleImpact;
        }
    }

    private void OnDisable()
    {
        if (jackpot777 != null)
        {
            jackpot777.JackpotStarted -= HandleJackpotStarted;
            jackpot777.JackpotEnded -= HandleJackpotEnded;
        }

        if (jackpotUI != null)
        {
            jackpotUI.Impacted -= HandleImpact;
        }

        StopAllEffectsImmediately();
    }

    private void HandleJackpotStarted(float duration)
    {
        PlayJackpotUI();
        StartOverlay();
        StartParticles();
        SetJackpotObjects(true);
    }

    private void HandleJackpotEnded()
    {
        StopOverlay();
        StopParticles();
        SetJackpotObjects(false);
    }

    private void PlayJackpotUI()
    {
        if (jackpotUI != null)
        {
            jackpotUI.Play();
        }
    }

    private void StartOverlay()
    {
        if (jackpotOverlay == null)
        {
            return;
        }

        overlaySequence?.Kill();
        jackpotOverlay.DOKill();

        jackpotOverlay.gameObject.SetActive(true);

        Color color = jackpotOverlay.color;
        color.a = minOverlayAlpha;
        jackpotOverlay.color = color;

        overlaySequence = DOTween.Sequence();

        overlaySequence.Append(
            jackpotOverlay.DOFade(
                maxOverlayAlpha,
                overlayDuration));

        overlaySequence.Append(
            jackpotOverlay.DOFade(
                minOverlayAlpha,
                overlayDuration));

        overlaySequence.SetLoops(
            -1,
            LoopType.Restart);

        overlaySequence.SetUpdate(true);

        overlaySequence.SetLink(
            gameObject,
            LinkBehaviour.KillOnDestroy);
    }

    private void StopOverlay()
    {
        overlaySequence?.Kill();
        overlaySequence = null;

        if (jackpotOverlay == null)
        {
            return;
        }

        jackpotOverlay.DOKill();

        jackpotOverlay
            .DOFade(0f, overlayFadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (jackpotOverlay != null
                    && jackpotOverlay.gameObject != gameObject)
                {
                    jackpotOverlay.gameObject.SetActive(false);
                }
            });
    }

    private void StartParticles()
    {
        if (jackpotParticles == null)
        {
            return;
        }

        foreach (ParticleSystem particle in jackpotParticles)
        {
            if (particle == null)
            {
                continue;
            }

            if (!particle.gameObject.activeSelf)
            {
                particle.gameObject.SetActive(true);
            }

            particle.Clear(true);
            particle.Play(true);
        }
    }

    private void StopParticles()
    {
        if (jackpotParticles == null)
        {
            return;
        }

        foreach (ParticleSystem particle in jackpotParticles)
        {
            if (particle == null)
            {
                continue;
            }

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            if (particle.gameObject != gameObject)
            {
                particle.gameObject.SetActive(false);
            }
        }
    }

    private void SetJackpotObjects(bool active)
    {
        if (jackpotObjects == null)
        {
            return;
        }

        foreach (GameObject target in jackpotObjects)
        {
            if (target != null && target != gameObject)
            {
                target.SetActive(active);
            }
        }
    }

    private void HandleImpact(
        JackpotImpactStrength impactStrength)
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(impactStrength);
        }
    }

    private void StopAllEffectsImmediately()
    {
        overlaySequence?.Kill();
        overlaySequence = null;

        if (jackpotOverlay != null)
        {
            jackpotOverlay.DOKill();

            Color color = jackpotOverlay.color;
            color.a = 0f;
            jackpotOverlay.color = color;

            if (jackpotOverlay.gameObject != gameObject)
            {
                jackpotOverlay.gameObject.SetActive(false);
            }
        }

        StopParticles();
        SetJackpotObjects(false);
    }
}