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
            StartCoroutine(ParryRoutine());
        }
    }

    IEnumerator ParryRoutine()
    {
        isParrying = true;

        Debug.Log("패링 시작!");

        yield return new WaitForSeconds(parryTime);

        isParrying = false;

        Debug.Log("패링 종료!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isParrying)
            return;

        if (!other.CompareTag("Bullet"))
            return;

        Debug.Log("패링 성공!");

        OnParrySuccess?.Invoke();

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.linearVelocity = -rb.linearVelocity;

        rb.linearVelocity = rb.linearVelocity.normalized * reflectSpeed;

        isParrying = false;
    }
}