using UnityEngine;

public class GamblerJackpotEffect : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private JackpotUI jackpotUI;

    [SerializeField]
    private CameraShake cameraShake;

    public void PlayJackpot()
    {
        if (jackpotUI != null)
        {
            jackpotUI.Play();
        }

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }
    }
}
