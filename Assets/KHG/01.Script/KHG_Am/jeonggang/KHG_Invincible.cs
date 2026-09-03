using System.Collections;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
    [SerializeField] private float postDashInvincibleTime = 1f; 

    private KHG_Dash dash;
    private int playerLayer;
    private int bulletLayer;

    private Coroutine invincibleCoroutine;
    private bool wasDashing = false;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();

        playerLayer = LayerMask.NameToLayer("Player");
        bulletLayer = LayerMask.NameToLayer("Bullet");
    }

    private void Update()
    {
        if (dash == null) return;

        if (dash.IsDashing && !wasDashing)
        {
            if (invincibleCoroutine != null)
            {
                StopCoroutine(invincibleCoroutine);
            }
            invincibleCoroutine = StartCoroutine(InvincibleRoutine());
        }

        wasDashing = dash.IsDashing;
    }

    private IEnumerator InvincibleRoutine()
    {
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, true);

        yield return new WaitUntil(() => !dash.IsDashing);

        yield return new WaitForSeconds(postDashInvincibleTime);

        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, false);

        invincibleCoroutine = null;
    }

    private void OnDisable()
    {
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, false);
        invincibleCoroutine = null;
        wasDashing = false;
    }
}