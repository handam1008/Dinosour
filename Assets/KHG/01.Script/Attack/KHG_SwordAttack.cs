using System.Collections;
using UnityEngine;

public class KHG_SwordAttack : MonoBehaviour
{
    [Header("공격 모션 설정")]
    [Tooltip("휘두를 각도 (음수: 시계방향 / 양수: 반시계방향)")]
    [SerializeField] private float swingAngle = -120f;

    [Tooltip("준비 동작(뒤로 살짝 빼는 각도)")]
    [SerializeField] private float windupAngle = 20f;

    [Header("속도 설정 (초 단위)")]
    [SerializeField] private float windupTime = 0.05f;  // 준비 동작 시간
    [SerializeField] private float swingTime = 0.08f;   // 베기 동작 시간
    [SerializeField] private float returnTime = 0.15f;  // 복귀 시간

    private Quaternion initialRotation;
    private bool isAttacking = false;

    private void Start()
    {
        // 대기 상태의 회전값 저장
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        // 마우스 좌클릭(0) 시에만 공격
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    private IEnumerator SwingRoutine()
    {
        isAttacking = true;

        Quaternion windupRotation = initialRotation * Quaternion.Euler(0, 0, windupAngle);
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, swingAngle);

        // 1단계: 선패기 (칼을 살짝 뒤로 뺌)
        float elapsedTime = 0f;
        while (elapsedTime < windupTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / windupTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, windupRotation, t);
            yield return null;
        }

        // 2단계: 빠른 베기
        elapsedTime = 0f;
        while (elapsedTime < swingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / swingTime;
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localRotation = Quaternion.Slerp(windupRotation, targetRotation, smoothT);
            yield return null;
        }

        // 3단계: 원위치 복귀
        elapsedTime = 0f;
        while (elapsedTime < returnTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / returnTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t);
            yield return null;
        }

        transform.localRotation = initialRotation;
        isAttacking = false;
    }
}
