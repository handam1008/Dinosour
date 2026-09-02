using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    [Header("패링 설정")]
    [SerializeField] private float parryTime = 0.2f;
    [SerializeField] private float reflectSpeed = 20f;

    [Header("패링 콜라이더")]
    [SerializeField] private Collider2D parryCollider;

    public bool isParrying = false;

    // 패링 성공했을 때 다른 스크립트에서 사용할 이벤트
    public event Action OnParrySuccess;

    private void Awake()
    {
        if (parryCollider == null)
        {
            parryCollider = GetComponent<Collider2D>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        // 이미 패링 중이면 무시
        if (isParrying)
            yield break;

        isParrying = true;

        Debug.Log("패링 시작");

        yield return new WaitForSeconds(parryTime);

        isParrying = false;

        Debug.Log("패링 종료");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 패링 중이 아니면 무시
        if (!isParrying)
            return;

        // Bullet 태그가 아니면 무시
        if (!other.CompareTag("Bullet"))
            return;

        Rigidbody2D bulletRb = other.attachedRigidbody;

        if (bulletRb == null)
        {
            Debug.LogWarning("패링한 Bullet에 Rigidbody2D가 없습니다.");
            return;
        }

        // 현재 총알의 이동 방향
        Vector2 incomingDirection = bulletRb.linearVelocity.normalized;

        // 총알이 움직이고 있지 않다면 무시
        if (incomingDirection == Vector2.zero)
        {
            Debug.LogWarning("총알의 속도가 0이라 패링할 수 없습니다.");
            return;
        }

        // 총알이 오던 방향의 정반대
        Vector2 reflectDirection = -incomingDirection;

        // 반대 방향으로 발사
        bulletRb.linearVelocity = reflectDirection * reflectSpeed;

        Debug.Log(
            "패링 성공! " +
            "들어온 방향 = " + incomingDirection +
            " / 반사 방향 = " + reflectDirection
        );

        // 패링 성공 이벤트 실행
        OnParrySuccess?.Invoke();
    }
}