using DG.Tweening;
using UnityEngine;



public class JackpotUI : MonoBehaviour
{
    [Header("Images")] [SerializeField] private RectTransform leftImage;
    [SerializeField] private RectTransform middleImage;
    [SerializeField] private RectTransform rightImage;

    [Header("Drop Movement")] [Min(0f)] [SerializeField]
    private float startHeight = 700f;

    [Min(0f)] [SerializeField] private float overshootDistance = 100f;

    [Min(0f)] [SerializeField] private float reboundHeight = 30f;

    [Min(0.01f)] [SerializeField] private float dropDuration = 0.16f;

    [Min(0.01f)] [SerializeField] private float reboundDuration = 0.1f;

    [Min(0.01f)] [SerializeField] private float settleDuration = 0.08f;

    [Min(0f)] [SerializeField] private float imageDelay = 0.15f;

    [Header("Finish")] [Min(0f)] [SerializeField]
    private float holdDuration = 0.8f;

    [Min(0.01f)] [SerializeField] private float fadeDuration = 0.6f;

    private CanvasGroup canvasGroup;
    private Sequence jackpotSequence;

    private Vector2 leftFinalPosition;
    private Vector2 middleFinalPosition;
    private Vector2 rightFinalPosition;

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
            Debug.LogError("Jackpot777UI에 이미지가 연결되지 않았습니다.", this);

            return;
        }

        // 기존 애니메이션이 실행 중이라면 정지한다.
        jackpotSequence?.Kill();

        ResetImagePositions();

        canvasGroup.alpha = 1f;

        jackpotSequence = DOTween.Sequence();

        // 영상의 등장 순서: 왼쪽 → 오른쪽 → 가운데
        AddDropAnimation(leftImage, leftFinalPosition.y, 0f);

        AddDropAnimation(rightImage, rightFinalPosition.y, imageDelay);

        AddDropAnimation(middleImage, middleFinalPosition.y, imageDelay * 2f);

        float oneDropDuration = dropDuration + reboundDuration + settleDuration;

        float allDropsFinishedTime = imageDelay * 2f + oneDropDuration;

        // 777을 잠깐 보여준 다음 전체를 사라지게 한다.
        jackpotSequence.Insert(allDropsFinishedTime + holdDuration,
            canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear));

        // 게임의 시간이 느려지거나 멈춰도 이 UI는 재생된다.
        jackpotSequence.SetUpdate(true);

        // JackpotEffect가 삭제되면 Tween도 함께 제거된다.
        jackpotSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        jackpotSequence.OnComplete(() =>
        {
            ResetImagePositions();
            jackpotSequence = null;
        });
    }

    private void AddDropAnimation(RectTransform image, float finalY, float startTime)
    {
        // 이미지를 최종 위치보다 위쪽으로 올려 놓는다.
        Vector2 startPosition = image.anchoredPosition;
        startPosition.y = finalY + startHeight;
        image.anchoredPosition = startPosition;

        Sequence dropSequence = DOTween.Sequence();

        // 1. 최종 위치보다 아래까지 빠르게 떨어진다.
        dropSequence.Append(image.DOAnchorPosY(finalY - overshootDistance, dropDuration).SetEase(Ease.InCubic));

        // 2. 위로 살짝 튕긴다.
        dropSequence.Append(image.DOAnchorPosY(finalY + reboundHeight, reboundDuration).SetEase(Ease.OutQuad));

        // 3. 원래 위치에 정착한다.
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



