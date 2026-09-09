using System;
using JJW.Script.Jackpot;
using UnityEngine;

public class DamageUpjackpot : MonoBehaviour
{
    private GamblerCoinProjectile coinProjectile;
    private JackpotDivision division;
    

    private void Awake()
    {
        coinProjectile = GetComponent<GamblerCoinProjectile>();
        division.DamageJackpot += OnCoinDamageUp;
    }

    private void OnCoinDamageUp(float damage)
    {
        coinProjectile.CoinDamageUp(damage);
    }
}
