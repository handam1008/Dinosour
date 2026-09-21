using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace NKY.Scripts.UI
{
    public class IslandZoomTransition : MonoBehaviour
    {
        [Header("카메라 설정")]
    [SerializeField] private CinemachineCamera mainCamera;
    [SerializeField] private float targetFOV = 20f;        // 줌인할 FOV (직교카메라는 orthographicSize 조절)
    [SerializeField] private float transitionDuration = 0.8f; // 연출 시간(초)

    [Header("속도선(Speed Lines) UI 설정")]
    [SerializeField] private CanvasGroup speedLinesCanvasGroup;
    [SerializeField] private RectTransform speedLinesRect;
    [SerializeField] private float targetLineScale = 1.4f;  // 빨려들어갈 때 속도선 스케일 변화

    private bool isTransitioning = false;

    private void Awake()
    {
        // 속도선 UI 초기화 (투명 상태)
        if (speedLinesCanvasGroup != null)
        {
            speedLinesCanvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// 섬 버튼의 OnClick 이벤트에 연결하여 호출하는 메서드
    /// </summary>
    public void OnIslandClicked(RectTransform islandUI)
    {
        if (isTransitioning) return;
        StartCoroutine(ZoomAndSpeedLineRoutine(islandUI));
    }

    private IEnumerator ZoomAndSpeedLineRoutine(RectTransform targetIsland)
    {
        isTransitioning = true;

        float elapsed = 0f;
        float startFOV = mainCamera.Lens.OrthographicSize;
        Vector3 startCamPos = mainCamera.transform.position;

        // 섬 버튼의 스크린 좌표를 월드 좌표로 변환하여 카메라 목표 위치 산출
        Vector3 targetWorldPos = targetIsland.position;
        targetWorldPos.z = startCamPos.z; // Z축 가심도 유지

        Vector3 startLinesScale = speedLinesRect != null ? speedLinesRect.localScale : Vector3.one;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // 가속감이 느껴지는 Ease-In 가공 (t^3)
            float easeT = t * t * t;

            // 1. 카메라 이동 및 Zoom-In
            if (mainCamera.Lens.Orthographic)
            {
                mainCamera.Lens.OrthographicSize = Mathf.Lerp(startFOV, targetFOV, easeT);
            }
            else
            {
                mainCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, easeT);
            }
            mainCamera.ForceCameraPosition(Vector3.Lerp(startCamPos, targetWorldPos, easeT), Quaternion.identity);

            // 2. 화면 사이드 속도선 알파/스케일 연출 (빨려 들어가는 효과)
            if (speedLinesCanvasGroup != null)
            {
                speedLinesCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeT);
            }
            if (speedLinesRect != null)
            {
                speedLinesRect.localScale = Vector3.Lerp(startLinesScale, Vector3.one * targetLineScale, easeT);
            }

            yield return null;
        }

        // 연출 완료 후 다음 씬 이동 또는 팝업 열기 로직 추가 영역
        // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("StageScene");
    }
    }
}