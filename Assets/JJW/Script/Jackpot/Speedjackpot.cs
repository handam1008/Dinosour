using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class Speedjackpot : MonoBehaviour
{
    private JackpotDivision division;
    private void Awake()
    {
        division = GetComponent<JackpotDivision>();
        division.SpeedJackpot += ApplySpeed;
    }

    public void ApplySpeed(PlayerController playerController, float amount, float duration)
    {
        playerController.ApplySpeed(amount, duration);
    }
}
