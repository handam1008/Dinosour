using System;
using System.Collections;
using System.Collections.Generic;
using JJW.Script.Jackpot;
using UnityEngine;

public class DamageUpjackpot : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damageUpCooldown = 10f;

    private readonly List<float> activeMultipliers = new List<float>();

    private GamblerCoinShooter coinShooter;
    private JackpotDivision division;
    private float baseDamageMultiplier = 1f;

    public bool IsActive => activeMultipliers.Count > 0;
    public float BuffDuration => damageUpCooldown;

    public event Action<bool> DamageStackAdded;
    public event Action DamageBuffEnded;

    private void Awake()
    {
        coinShooter = GetComponentInChildren<GamblerCoinShooter>();
        division = GetComponent<JackpotDivision>();

        if (coinShooter != null)
        {
            baseDamageMultiplier = coinShooter.DamageMultiplier;
        }
    }

    private void OnEnable()
    {
        if (division != null)
        {
            division.DamageJackpot += OnCoinDamageUp;
        }
    }

    private void OnDisable()
    {
        if (division != null)
        {
            division.DamageJackpot -= OnCoinDamageUp;
        }

        StopAllCoroutines();

        bool wasActive = IsActive;

        activeMultipliers.Clear();
        RefreshDamageMultiplier();

        if (wasActive)
        {
            DamageBuffEnded?.Invoke();
        }
    }

    private void OnCoinDamageUp(float multiplier)
    {
        if (coinShooter == null)
        {
            return;
        }

        bool wasAlreadyActive = IsActive;

        activeMultipliers.Add(Mathf.Max(0f, multiplier));
        RefreshDamageMultiplier();

        DamageStackAdded?.Invoke(wasAlreadyActive);

        StartCoroutine(RemoveMultiplierAfterTime(
            multiplier,
            damageUpCooldown));
    }

    private IEnumerator RemoveMultiplierAfterTime(
        float multiplier,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        activeMultipliers.Remove(multiplier);
        RefreshDamageMultiplier();

        if (!IsActive)
        {
            DamageBuffEnded?.Invoke();
        }
    }

    private void RefreshDamageMultiplier()
    {
        if (coinShooter == null)
        {
            return;
        }

        float finalMultiplier = baseDamageMultiplier;

        foreach (float multiplier in activeMultipliers)
        {
            finalMultiplier *= multiplier;
        }

        coinShooter.CoinDamageUp(finalMultiplier);
    }
}