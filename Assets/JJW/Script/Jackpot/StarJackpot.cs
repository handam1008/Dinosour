using System;
using System.Collections;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class StarJackpot
    : MonoBehaviour, IIncomingDamageModifier
{
    private JackpotDivision division;
    private Coroutine invincibleCoroutine;

    public bool IsInvincible { get; private set; }

    public int Priority => 10000;

    public event Action<float> InvincibilityStarted;
    public event Action InvincibilityEnded;

    private void Awake()
    {
        division = GetComponent<JackpotDivision>();

        if (division == null)
        {
            division = GetComponentInParent<JackpotDivision>();
        }
    }

    private void OnEnable()
    {
        if (division != null)
        {
            division.StarJackpot += StartInvincibility;
        }
    }

    private void OnDisable()
    {
        if (division != null)
        {
            division.StarJackpot -= StartInvincibility;
        }

        StopInvincibility();
    }

    public float ModifyIncomingDamage(
        DamageRequest request,
        float currentAmount)
    {
        if (IsInvincible)
        {
            return 0f;
        }

        return currentAmount;
    }

    private void StartInvincibility(float duration)
    {
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
        }

        IsInvincible = true;
        InvincibilityStarted?.Invoke(duration);

        invincibleCoroutine =
            StartCoroutine(InvincibilityCoroutine(duration));
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        invincibleCoroutine = null;
        EndInvincibility();
    }

    private void StopInvincibility()
    {
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = null;
        }

        EndInvincibility();
    }

    private void EndInvincibility()
    {
        if (!IsInvincible)
        {
            return;
        }

        IsInvincible = false;
        InvincibilityEnded?.Invoke();
    }
}