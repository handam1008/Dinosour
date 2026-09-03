using System.Collections;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
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
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, true);

        yield return new WaitUntil(() => !dash.IsDashing);

        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, false);

        invincibleCoroutine = null;
    }

    private void OnDisable()
    {
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, false);
        invincibleCoroutine = null;
    }
}