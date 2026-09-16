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
    private float originalMaxHealth;
    private bool hasOriginalMaxHealth;

    public bool IsActive { get; private set; }

    public event Action<float> JackpotStarted;
    public event Action JackpotEnded;

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

        jackpotCoroutine = StartCoroutine(
            JackpotCoroutine(amount, cooldown));
    }

    private IEnumerator JackpotCoroutine(
        float amount,
        float cooldown)
    {
        IsActive = true;
        IncreaseMaxHealth();
        JackpotStarted?.Invoke(cooldown);

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

        FinishJackpot();
    }

    private void IncreaseMaxHealth()
    {
        if (TryGetComponent<Health>(out var health))
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

        if (TryGetComponent<Health>(out var health))
        {
            health.maxHealth = originalMaxHealth;
        }

        hasOriginalMaxHealth = false;
    }

    private void FinishJackpot()
    {
        RestoreMaxHealth();

        bool wasActive = IsActive;

        IsActive = false;
        jackpotCoroutine = null;

        if (wasActive)
        {
            JackpotEnded?.Invoke();
        }
    }

    private void StopJackpot()
    {
        if (jackpotCoroutine != null)
        {
            StopCoroutine(jackpotCoroutine);
            jackpotCoroutine = null;
        }

        bool wasActive = IsActive;

        RestoreMaxHealth();
        IsActive = false;

        if (wasActive)
        {
            JackpotEnded?.Invoke();
        }
    }
}
