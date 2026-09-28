using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShieldPulse : MonoBehaviour
{
    [SerializeField] private float minScale = 1.3f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float pulseDuration = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float minAlpha = 0.45f;

    [SerializeField, Range(0f, 1f)]
    private float maxAlpha = 0.75f;

    private SpriteRenderer spriteRenderer;
    private Sequence pulseSequence;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayPulse();
    }

    private void OnDisable()
    {
        pulseSequence?.Kill();
        pulseSequence = null;

        transform.localScale = originalScale;
    }

    private void PlayPulse()
    {
        pulseSequence?.Kill();

        transform.localScale = originalScale * minScale;
        SetAlpha(minAlpha);

        pulseSequence = DOTween.Sequence();

        pulseSequence.Append(transform.DOScale(originalScale * maxScale, pulseDuration).SetEase(Ease.InOutSine));

        pulseSequence.Join(spriteRenderer.DOFade(maxAlpha, pulseDuration).SetEase(Ease.InOutSine));

        pulseSequence.Append(
            transform.DOScale(originalScale * minScale, pulseDuration).SetEase(Ease.InOutSine));

        pulseSequence.Join(spriteRenderer.DOFade(minAlpha, pulseDuration).SetEase(Ease.InOutSine));

        pulseSequence.SetLoops(-1, LoopType.Restart);

        pulseSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void SetAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}