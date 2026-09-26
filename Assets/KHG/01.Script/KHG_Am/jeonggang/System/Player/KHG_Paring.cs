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

    [Header("패링 애니메이션")]
    [SerializeField] private Animator parryAnimator;
    [SerializeField] private string parryTriggerName = "Parry";

    public bool IsParrying => isParrying;

    private bool isParrying;
    private bool isCooldown;

    private readonly HashSet<Rigidbody2D> reflectedBullets = new();

    public event Action OnParrySuccess;

    private void Update()
    {
        // 키보드 입력은 이 스크립트에서만 처리
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        // 패링 중이면 무시
        if (isParrying)
            return;

        // 쿨타임 중이면 무시
        if (isCooldown)
            return;

        StartCoroutine(ParryRoutine());
    }

    private void FixedUpdate()
    {
        if (!isParrying)
            return;

        CheckParryBox();
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        isCooldown = true;

        reflectedBullets.Clear();

        Debug.Log("패링 시작");

        // 패링 애니메이션
        if (parryAnimator != null)
        {
            parryAnimator.SetTrigger(parryTriggerName);
        }

        // 패링 판정 시간
        yield return new WaitForSeconds(parryTime);

        isParrying = false;

        Debug.Log("패링 판정 종료");

        // 패링 판정 시간을 제외한 나머지 쿨타임
        float remainingCooldown =
            Mathf.Max(0f, parryCooldown - parryTime);

        if (remainingCooldown > 0f)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        isCooldown = false;

        Debug.Log("패링 쿨타임 완료");
    }

    private void CheckParryBox()
    {
        if (swordTransform == null)
            return;

        Vector2 boxCenter =
            swordTransform.TransformPoint(parryBoxOffset);

        float boxAngle =
            swordTransform.eulerAngles.z;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                boxCenter,
                parryBoxSize,
                boxAngle,
                bulletLayer
            );

        foreach (Collider2D other in hits)
        {
            // Bullet 태그가 아니면 무시
            if (!other.CompareTag("Bullet"))
                continue;

            Rigidbody2D bulletRb =
                other.attachedRigidbody;

            if (bulletRb == null)
                continue;

            if (reflectedBullets.Contains(bulletRb))
                continue;

            reflectedBullets.Add(bulletRb);

            Vector2 reflectDirection =
                -bulletRb.linearVelocity.normalized;

            if (reflectDirection == Vector2.zero)
            {
                reflectDirection =
                    -swordTransform.right;
            }

            bulletRb.linearVelocity =
                reflectDirection * reflectSpeed;

            Debug.Log("패링 성공!");

            OnParrySuccess?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swordTransform == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3 boxCenter =
            swordTransform.TransformPoint(parryBoxOffset);

        Gizmos.matrix =
            Matrix4x4.TRS(
                boxCenter,
                swordTransform.rotation,
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            parryBoxSize
        );

        Gizmos.matrix =
            Matrix4x4.identity;
    }
}