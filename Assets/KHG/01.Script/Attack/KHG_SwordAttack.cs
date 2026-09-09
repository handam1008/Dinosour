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
    [SerializeField] private float windupTime = 0.3f;  // 준비 동작 시간
    [field: SerializeField] public float SwingTime = 0.4f;   // 베기 동작 시간
    [field: SerializeField] public float ReturnTime = 0.2f;  // 복귀 시간

    private Quaternion initialRotation;
    private bool isAttacking = false;

    private void Start()
    {
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
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

        float elapsedTime = 0f;
        while (elapsedTime < windupTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / windupTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, windupRotation, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < SwingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / SwingTime;
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localRotation = Quaternion.Slerp(windupRotation, targetRotation, smoothT);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < ReturnTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / ReturnTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t);
            yield return null;
        }

        transform.localRotation = initialRotation;
        isAttacking = false;
    }
}
