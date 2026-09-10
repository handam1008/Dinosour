using System;
using System.Collections;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class Jackpot777 : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float healInterval = 0.2f;
    private JackpotDivision division;
    private Coroutine jackpotCoroutine;
    private bool isJackpotActive;
    private float originalMaxHealth;
    private bool hasOriginalMaxHealth;

    private void Awake()
    {
        division = GetComponent<JackpotDivision>();
    }

    private void OnEnable()
    {
        if (division != null)
        {
            division.Jackpot777 += OnJackpot777;
        }
    }

    private void OnDisable()
    {
        if (division != null)
        {
            division.Jackpot777 -= OnJackpot777;
        }

        StopJackpot();
    }

    public void OnJackpot777(float amount, float cooldown)
    {
        if (jackpotCoroutine != null)
        {
            return;
        }

        jackpotCoroutine = StartCoroutine(JackpotCoroutine(amount, cooldown));
    }

    private IEnumerator JackpotCoroutine(float amount, float cooldown)
    {
        isJackpotActive = true;

        IncreaseMaxHealth();

        float remainingTime = cooldown;

        while (remainingTime >= healInterval)
        {
            yield return new WaitForSeconds(healInterval);

            remainingTime -= healInterval;

            if (TryGetComponent<IHealable>(out var healable))
            {
                healable.Heal(amount);
            }
        }

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        RestoreMaxHealth();

        isJackpotActive = false;
        jackpotCoroutine = null;
    }

    private void IncreaseMaxHealth()
    {
        if (TryGetComponent(out Health health))
        {
            originalMaxHealth = health.maxHealth;
            health.maxHealth *= 2f;
            hasOriginalMaxHealth = true;
        }
    }

    private void RestoreMaxHealth()
    {
        if (!hasOriginalMaxHealth)
        {
            return;
        }

        if (TryGetComponent(out Health health))
        {
            health.maxHealth = originalMaxHealth;
        }

        hasOriginalMaxHealth = false;
    }

    private void StopJackpot()
    {
        if (jackpotCoroutine != null)
        {
            StopCoroutine(jackpotCoroutine);
            jackpotCoroutine = null;
        }

        if (!isJackpotActive)
        {
            return;
        }

        RestoreMaxHealth();
        isJackpotActive = false;
    }
}
