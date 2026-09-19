using UnityEngine;
using UnityEngine.EventSystems;

namespace NKY.Scripts.UI
{
    public class UIIslandFloat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
[Header("부유 효과 (Floating)")]
    [SerializeField] private float floatSpeed = 2.0f;     // 수면 위 흔들림 속도
    [SerializeField] private float floatAmount = 10.0f;   // 상하 이동 폭 (px)

    [Header("스케일 효과 (Scale)")]
    [SerializeField] private float hoverScale = 1.1f;     // 마우스 올렸을 때 비율
    [SerializeField] private float clickScale = 0.92f;    // 누르고 있을 때 비율
    [SerializeField] private float lerpSpeed = 12.0f;     // 스케일 변화 보간 속도

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Vector3 targetScale;

    private float timeOffset;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        basePosition = rectTransform.anchoredPosition;
        baseScale = transform.localScale;
        targetScale = baseScale;

        // 섬마다 무작위 오프셋을 두어 모든 섬이 동시에 똑같이 흔들리지 않도록 설정
        timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // 1. 부유 애니메이션 (Sin 파형)
        float newY = basePosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmount;
        rectTransform.anchoredPosition = new Vector2(basePosition.x, newY);

        // 2. 상태에 따른 목표 스케일 지정
        if (isPressed)
        {
            targetScale = baseScale * clickScale;
        }
        else if (isHovered)
        {
            targetScale = baseScale * hoverScale;
        }
        else
        {
            targetScale = baseScale;
        }

        // 3. 스케일 부드럽게 전환
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 상태 초기화
        isHovered = false;
        isPressed = false;
        if (rectTransform != null)
        {
            transform.localScale = baseScale;
            rectTransform.anchoredPosition = basePosition;
        }
    }
    }
}