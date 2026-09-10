using System;
using JJW.Script.Jackpot;
using System.Collections;
using UnityEngine;

public class DamageUpjackpot : MonoBehaviour
{
    private GamblerCoinShooter coinShooter;
    private JackpotDivision division;
    [SerializeField] private float damageUpCooldwon = 10f;
    private void Awake()
    {
        coinShooter =  GetComponentInChildren<GamblerCoinShooter>();
        division = GetComponent<JackpotDivision>();
    }

    private void OnEnable()
    {
        division.DamageJackpot += OnCoinDamageUp;
    }

    private void OnDisable()
    {
        division.DamageJackpot -= OnCoinDamageUp;
    }

    private void OnCoinDamageUp(float amount)
    {
        float currentDamageMultiple = coinShooter.DamageMultiplier;
        coinShooter.CoinDamageUp(amount);
        StartCoroutine(DamageUpCooldown(damageUpCooldwon, currentDamageMultiple));
    }

    private IEnumerator DamageUpCooldown(float cooldown, float amount)
    {
        yield return new WaitForSeconds(cooldown);
        coinShooter.CoinDamageUp(amount);
        
    }
}
