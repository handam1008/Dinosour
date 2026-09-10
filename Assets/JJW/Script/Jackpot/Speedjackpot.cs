using System;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class Speedjackpot : MonoBehaviour
{
    private JackpotDivision division;
    private void Awake()
    {
        division = GetComponent<JackpotDivision>();
    }

    private void OnEnable()
    {
        division.SpeedJackpot += ApplySpeed;
    }

    private void OnDisable()
    {
        division.SpeedJackpot -= ApplySpeed;
    }

    public void ApplySpeed( float amount, float duration)
    {
        if (TryGetComponent(out ISpeedable speedable))
        {
            speedable.ApplySpeed(amount, duration);
        }
    }
}
