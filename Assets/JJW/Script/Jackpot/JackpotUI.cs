using System;
using DG.Tweening;
using UnityEngine;

public enum JackpotImpactStrength
{
    Medium,
    Strong
}
public class JackpotUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private RectTransform leftImage;
    [SerializeField] private RectTransform middleImage;
    [SerializeField] private RectTransform rightImage;

    [Header("Drop Movement")]
    [Min(0f)]
    [SerializeField] private float startHeight = 700f;

    [Min(0f)]
    [SerializeField] private float overshootDistance = 100f;

    [Min(0f)]
    [SerializeField] private float reboundHeight = 30f;

    [Min(0.01f)]
    [SerializeField] private float dropDuration = 0.16f;

    [Min(0.01f)]
    [SerializeField] private float reboundDuration = 0.1f;

    [Min(0.01f)]
    [SerializeField] private float settleDuration = 0.08f;

    [Min(0f)]
    [SerializeField] private float imageDelay = 0.15f;

    [Header("Finish")]
    [Min(0f)]
    [SerializeField] private float holdDuration = 0.55f;

    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.28f;

    private CanvasGroup canvasGroup;
    private Sequence jackpotSequence;

    private Vector2 leftFinalPosition;
    private Vector2 middleFinalPosition;
    private Vector2 rightFinalPosition;

    // 숫자가 착지했음을 외부에 알리는 이벤트
    public event Action<JackpotImpactStrength> Impacted;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        SaveFinalPositions();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Play()
    {
        if (leftImage == null ||
            middleImage == null ||
            rightImage == null)
        {
            Debug.LogError("JackpotUI에 이미지가 연결되지 않았습니다.", this);

            return;
        }

        jackpotSequence?.Kill();

        ResetImagePositions();
        canvasGroup.alpha = 1f;

        jackpotSequence = DOTween.Sequence();

        // 왼쪽: 중간 충격
        AddDropAnimation(leftImage, leftFinalPosition.y, 0f, JackpotImpactStrength.Medium);

        // 오른쪽: 중간 충격
        AddDropAnimation(rightImage, rightFinalPosition.y, imageDelay, JackpotImpactStrength.Medium);

        // 가운데: 강한 충격
        AddDropAnimation(middleImage, middleFinalPosition.y, imageDelay * 2f, JackpotImpactStrength.Strong);

        float oneDropDuration = dropDuration + reboundDuration + settleDuration;

        float allDropsFinishedTime = imageDelay * 2f + oneDropDuration;

        jackpotSequence.Insert(allDropsFinishedTime + holdDuration,canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear));

        jackpotSequence.SetUpdate(true);

        jackpotSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        jackpotSequence.OnComplete(() =>
        {
            ResetImagePositions();
            jackpotSequence = null;
        });
    }

    private void AddDropAnimation(
        RectTransform image,
        float finalY,
        float startTime,
        JackpotImpactStrength impactStrength)
    {
        Vector2 startPosition = image.anchoredPosition;
        startPosition.y = finalY + startHeight;
        image.anchoredPosition = startPosition;

        Sequence dropSequence = DOTween.Sequence();

        // 최종 위치보다 아래까지 떨어진다.
        dropSequence.Append(image.DOAnchorPosY(finalY - overshootDistance, dropDuration).SetEase(Ease.InCubic));

        // 가장 아래까지 내려온 바로 이 순간에 착지 이벤트 발생
        dropSequence.AppendCallback(() =>
        {
            Impacted?.Invoke(impactStrength);
        });

        // 위로 살짝 튕긴다.
        dropSequence.Append(
            image.DOAnchorPosY(finalY + reboundHeight, reboundDuration).SetEase(Ease.OutQuad));

        // 원래 위치에 정착한다.
        dropSequence.Append(image.DOAnchorPosY(finalY, settleDuration).SetEase(Ease.InOutQuad));

        jackpotSequence.Insert(startTime, dropSequence);
    }

    private void SaveFinalPositions()
    {
        if (leftImage != null)
            leftFinalPosition = leftImage.anchoredPosition;

        if (middleImage != null)
            middleFinalPosition = middleImage.anchoredPosition;

        if (rightImage != null)
            rightFinalPosition = rightImage.anchoredPosition;
    }

    private void ResetImagePositions()
    {
        if (leftImage != null)
            leftImage.anchoredPosition = leftFinalPosition;

        if (middleImage != null)
            middleImage.anchoredPosition = middleFinalPosition;

        if (rightImage != null)
            rightImage.anchoredPosition = rightFinalPosition;
    }

    private void OnDestroy()
    {
        jackpotSequence?.Kill();
    }
}



