using SSW;
using System.Collections;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
    private KHG_Dash Khg_Dash;

    private int playerLayer;
    private int bulletLayer;

    private Coroutine invCoroutine;

    private void Awake()
    {
        Khg_Dash = GetComponent<KHG_Dash>();

        playerLayer = LayerMask.NameToLayer("Player");
        bulletLayer = LayerMask.NameToLayer("Bullet");
    }

    public void Invincible()
    {
        if (Khg_Dash == null)
        {
            Debug.LogWarning("[KHG_Invincible] KHG_Dash가 없습니다.");
            return;
        }

        if (invCoroutine != null)
        {
            StopCoroutine(invCoroutine);

            Physics2D.IgnoreLayerCollision(
                playerLayer,
                bulletLayer,
                false
            );
        }

        invCoroutine = StartCoroutine(InvCoroutine());
    }

    private IEnumerator InvCoroutine()
    {
        // 플레이어 ↔ 총알 충돌 무시
        Physics2D.IgnoreLayerCollision(
            playerLayer,
            bulletLayer,
            true
        );

        Debug.Log("[KHG_Invincible] ★ 대쉬 무적 시작 ★");

        yield return new WaitForSeconds(Khg_Dash._dashDuration);

        // 충돌 복구
        Physics2D.IgnoreLayerCollision(
            playerLayer,
            bulletLayer,
            false
        );

        Debug.Log("[KHG_Invincible] ★ 대쉬 무적 종료 ★");

        invCoroutine = null;
    }

    private void OnDisable()
    {
        if (invCoroutine != null)
        {
            StopCoroutine(invCoroutine);
            invCoroutine = null;
        }

        if (playerLayer != -1 && bulletLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(
                playerLayer,
                bulletLayer,
                false
            );
        }
    }
}