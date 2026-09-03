using System.Collections;
using SSW;
using UnityEngine;

public class KDH_PoisonStatus : MonoBehaviour
{
    private Coroutine poisonRoutine;
    private int remainingTicks;
    private float tickInterval;
    private float tickDamage;
    private IDamageable damageable;

    private void Awake()
    {
        TryGetComponent(out damageable);
    }

    public void ApplyPoison(int dotCount, float duration, float damage)
    {
        tickInterval = duration / dotCount;
        tickDamage = damage;

        if (poisonRoutine != null)
        {
            remainingTicks = Mathf.Max(remainingTicks, dotCount);
        }
        else
        {
            remainingTicks = dotCount;
            poisonRoutine = StartCoroutine(PoisonRoutine());
        }
    }

    private IEnumerator PoisonRoutine()
    {
        while (remainingTicks > 0)
        {
            yield return new WaitForSeconds(tickInterval);
            remainingTicks--;
            damageable?.TakeDamage(tickDamage);
        }

        poisonRoutine = null;
    }
}