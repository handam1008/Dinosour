using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    [Header("패링 설정")]
    [SerializeField] private float parryTime = 3f;
    [SerializeField] private float reflectSpeed = 20f;
    [SerializeField] private float parryCooldown = 12f; // 쿨타임 시간(초)

    [Header("패링 콜라이더")]
    [SerializeField] private Collider2D parryCollider;

    public bool isParrying = false;
    private bool isCooldown = false; 

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
            Keyboard.current.eKey.wasPressedThisFrame &&
            !isCooldown && !isParrying)
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        isCooldown = true; // 쿨타임 시작

        Debug.Log("패링 시작");

        yield return new WaitForSeconds(parryTime);

        isParrying = false;

        Debug.Log("패링 판정 종료 (쿨타임 시작)");

        float remainingCooldown = parryCooldown - parryTime;
        if (remainingCooldown > 0f)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        isCooldown = false; 
        Debug.Log("패링 쿨타임 완료");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isParrying)
            return;

        if (!other.CompareTag("Bullet"))
            return;

        Rigidbody2D bulletRb = other.attachedRigidbody;

        if (bulletRb == null)
        {
            Debug.LogWarning("패링한 Bullet에 Rigidbody2D가 없습니다.");
            return;
        }

        Vector2 incomingDirection = bulletRb.linearVelocity.normalized;

        if (incomingDirection == Vector2.zero)
        {
            Debug.LogWarning("총알의 속도가 0이라 패링할 수 없습니다.");
            return;
        }

        Vector2 reflectDirection = -incomingDirection;

        bulletRb.linearVelocity = reflectDirection * reflectSpeed;

        Debug.Log(
            "패링 성공! " +
            "들어온 방향 = " + incomingDirection +
            " / 반사 방향 = " + reflectDirection
        );

        OnParrySuccess?.Invoke();
    }
}