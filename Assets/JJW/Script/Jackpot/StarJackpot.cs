using System.Collections;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class StarJackpot : MonoBehaviour
{
    private JackpotDivision division;
    private Health health;
    private Coroutine starCoroutine;
    private float originalMaxHealth;
    private bool hasOriginalMaxHealth;

    private void Awake()
    {
        division = GetComponent<JackpotDivision>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (division != null)
        {
            division.StarJackpot += OnStar;
        }
    }

    private void OnDisable()
    {
        if (division != null)
        {
            division.StarJackpot -= OnStar;
        }

        StopStar();
    }

    private void OnStar(float cooldown)
    {
        StopStar();

        if (health == null)
        {
            return;
        }

        originalMaxHealth = health.maxHealth;
        hasOriginalMaxHealth = true;

        health.maxHealth = 10000000000000f;

        starCoroutine = StartCoroutine(StarCoroutine(cooldown));
    }

    private IEnumerator StarCoroutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);

        RestoreMaxHealth();
        starCoroutine = null;
    }

    private void RestoreMaxHealth()
    {
        if (!hasOriginalMaxHealth || health == null)
        {
            return;
        }

        health.maxHealth = originalMaxHealth;
        hasOriginalMaxHealth = false;
    }

    private void StopStar()
    {
        if (starCoroutine != null)
        {
            StopCoroutine(starCoroutine);
            starCoroutine = null;
        }

        RestoreMaxHealth();
    }
    // 셋  엑티브로 샌드백으로 켰다 껐다 핫기
    
}
