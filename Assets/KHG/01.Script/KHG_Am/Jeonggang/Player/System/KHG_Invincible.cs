using System.Collections;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
    [Header("대쉬 무적 설정")]
    [SerializeField] private float invincibleDuration = 0.2f;

    private KHG_Dash dash;

    private int playerLayer;
    private int bulletLayer;

    private Coroutine invincibleCoroutine;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();

        playerLayer = LayerMask.NameToLayer("Player");
        bulletLayer = LayerMask.NameToLayer("Bullet");
    }

    private void Update()
    {
        if (dash != null && dash.IsDashing)
        {
            if (invincibleCoroutine == null)
            {
                invincibleCoroutine = StartCoroutine(InvincibleRoutine());
            }
        }
    }

    private IEnumerator InvincibleRoutine()
    {
        Physics2D.IgnoreLayerCollision(
            playerLayer,
            bulletLayer,
            true
        );

        yield return new WaitForSeconds(invincibleDuration);

        Physics2D.IgnoreLayerCollision(
            playerLayer,
            bulletLayer,
            false
        );

        invincibleCoroutine = null;
        Debug.Log("성공");
    }

    private void OnDisable()
    {
        Physics2D.IgnoreLayerCollision(
            playerLayer,
            bulletLayer,
            false
        );
    }
}