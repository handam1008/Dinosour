using System;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class Jackpot444 : MonoBehaviour
{
    private JackpotDivision division;

    private void Awake()
    {
        division = GetComponent<JackpotDivision>();
    }

    private void OnEnable()
    {
        division.Jackpot444 += Killme;
    }

    private void OnDisable()
    {
        division.Jackpot444 -= Killme;
    }

    private void Killme(float damage)
    {
        if (TryGetComponent(out IDamageable healable))
        {
            healable.TakeDamage(damage);
        }
    }
}
