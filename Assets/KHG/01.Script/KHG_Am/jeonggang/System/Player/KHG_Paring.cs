using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    [Header("패링 설정")]
    [SerializeField] private float parryTime = 0.3f;
    [SerializeField] private float reflectSpeed = 20f;
    [SerializeField] private float parryCooldown = 1.2f;

    [Header("패링 Overlap Box")]
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Vector2 parryBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 parryBoxSize = new Vector2(2f, 0.5f);
    [SerializeField] private LayerMask bulletLayer;

    public bool isParrying = false;
    private bool isCooldown = false;

    private readonly HashSet<Rigidbody2D> reflectedBullets = new();

    public event Action OnParrySuccess;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            !isCooldown &&
            !isParrying)
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (isParrying)
            CheckParryBox();
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        isCooldown = true;
        reflectedBullets.Clear();

        Debug.Log("패링 시작");

        yield return new WaitForSeconds(parryTime);

        isParrying = false;

        Debug.Log("패링 판정 종료");

        float remainingCooldown = parryCooldown - parryTime;

        if (remainingCooldown > 0f)
            yield return new WaitForSeconds(remainingCooldown);

        isCooldown = false;

        Debug.Log("패링 쿨타임 완료");
    }

    private void CheckParryBox()
    {
        if (swordTransform == null)
        {
            Debug.LogWarning("Sword Transform을 Inspector에 지정하세요.");
            return;
        }

        Vector2 boxCenter = swordTransform.TransformPoint(parryBoxOffset);
        float boxAngle = swordTransform.eulerAngles.z;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            boxCenter,
            parryBoxSize,
            boxAngle,
            bulletLayer
        );

        foreach (Collider2D other in hits)
        {
            if (!other.CompareTag("Bullet"))
                continue;

            Rigidbody2D bulletRb = other.attachedRigidbody;

            if (bulletRb == null || reflectedBullets.Contains(bulletRb))
                continue;

            reflectedBullets.Add(bulletRb);

            Vector2 reflectDirection = -bulletRb.linearVelocity.normalized;

            if (reflectDirection == Vector2.zero)
                reflectDirection = -swordTransform.right;

            bulletRb.linearVelocity = reflectDirection * reflectSpeed;

            Debug.Log($"패링 성공! 반사 방향 = {reflectDirection}");
            OnParrySuccess?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swordTransform == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3 boxCenter = swordTransform.TransformPoint(parryBoxOffset);

        Gizmos.matrix = Matrix4x4.TRS(
            boxCenter,
            swordTransform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, parryBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}