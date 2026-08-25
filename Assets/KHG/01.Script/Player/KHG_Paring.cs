using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    [Header("패링 설정")]
    public bool isParrying = false;
    public float parryTime = 0.2f;
    public float reflectSpeed = 20f;

    public Action OnParrySuccess;

    private Coroutine parryCoroutine;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Parry();
        }
    }

    public void Parry()
    {
        if (!isParrying)
        {
            if (parryCoroutine != null) StopCoroutine(parryCoroutine);
            parryCoroutine = StartCoroutine(ParryRoutine());
        }
    }

    IEnumerator ParryRoutine()
    {
        isParrying = true;
        Debug.Log("[KHG_Paring] 패링 판정 시작 (E키 입력됨)");

        yield return new WaitForSeconds(parryTime);

        isParrying = false;
        Debug.Log("[KHG_Paring] 패링 판정 종료 (시간 초과)");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isParrying)
        {
            Debug.Log($"[KHG_Paring] {other.name}과 충돌했지만 패링 중이 아님 (!isParrying)");
            return;
        }

        if (!other.CompareTag("Bullet"))
        {
            Debug.Log($"[KHG_Paring] 충돌한 {other.name}의 태그가 'Bullet'이 아님 (현재 태그: {other.tag})");
            return;
        }

        Debug.Log("[KHG_Paring] ★ 패링 성공! ★");

        OnParrySuccess?.Invoke();

        if (parryCoroutine != null) StopCoroutine(parryCoroutine);
        isParrying = false;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("[KHG_Paring] 총알에 Rigidbody2D가 없습니다.");
            return;
        }

        rb.linearVelocity = -rb.linearVelocity;
        rb.linearVelocity = rb.linearVelocity.normalized * reflectSpeed;
    }
}