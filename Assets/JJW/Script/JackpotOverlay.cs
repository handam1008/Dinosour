using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class JackpotOverlay : MonoBehaviour
{
   [Header("References")]
    [SerializeField] private Image jackpotOverlay;
    [SerializeField] private Gamblinger gamblinger;

    [Header("Overlay Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float minOverlayAlpha = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float maxOverlayAlpha = 0.75f;

    [Header("Fade Time")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.6f;

    private Sequence jackpotSequence;
    private Tween fadeOutTween;

    private void Awake()
    {
        if (gamblinger == null)
        {
            gamblinger = GetComponent<Gamblinger>();
        }

        SetOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        if (gamblinger == null)
            return;

        gamblinger.OnJackpot += OnOverlay;
        gamblinger.OnJackpotEnd += KillDoT;
    }

    private void OnDisable()
    {
        if (gamblinger != null)
        {
            gamblinger.OnJackpot -= OnOverlay;
            gamblinger.OnJackpotEnd -= KillDoT;
        }

        StopAllTweens();
        SetOverlayAlpha(0f);
    }

    private void OnOverlay()
    {
        if (jackpotOverlay == null)
            return;

        StopAllTweens();

        // 항상 최소 알파값에서 시작한다.
        SetOverlayAlpha(minOverlayAlpha);

        jackpotSequence = DOTween.Sequence();

        // 최소 알파값에서 최대 알파값까지 변화한다.
        jackpotSequence.Append(jackpotOverlay.DOFade(maxOverlayAlpha, fadeDuration).SetEase(Ease.InOutSine));

        // 최대 → 최소 → 최대를 계속 반복한다.
        jackpotSequence.SetLoops(-1, LoopType.Yoyo);

        // Time Scale이 0이어도 재생된다.
        jackpotSequence.SetUpdate(true);

        // 이 오브젝트가 파괴되면 Tween도 제거된다.
        jackpotSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void KillDoT()
    {
        if (jackpotOverlay == null)
            return;

        jackpotSequence?.Kill();
        jackpotSequence = null;

        fadeOutTween?.Kill();
        jackpotOverlay.DOKill();

        // 현재 알파값에서 완전히 투명해진다.
        fadeOutTween = jackpotOverlay.DOFade(0f, fadeDuration).SetEase(Ease.OutSine).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        fadeOutTween.OnComplete(() =>
        {
            fadeOutTween = null;
        });
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (jackpotOverlay == null)
            return;

        Color overlayColor = jackpotOverlay.color;
        overlayColor.a = alpha;
        jackpotOverlay.color = overlayColor;
    }

    private void StopAllTweens()
    {
        jackpotSequence?.Kill();
        jackpotSequence = null;

        fadeOutTween?.Kill();
        fadeOutTween = null;

        if (jackpotOverlay != null)
        {
            jackpotOverlay.DOKill();
        }
    }
}
