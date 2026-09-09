using UnityEngine;

public class GamblerJackpotEffect : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private JackpotUI jackpotUI;

    [SerializeField]
    private CameraShake cameraShake;

    private void OnEnable()
    {
        if (jackpotUI != null)
        {
            jackpotUI.Impacted += HandleImpact;
        }
    }

    private void OnDisable()
    {
        if (jackpotUI != null)
        {
            jackpotUI.Impacted -= HandleImpact;
        }
    }

    public void PlayJackpot()
    {
        if (jackpotUI != null)
        {
            jackpotUI.Play();
        }
    }

    private void HandleImpact(
        JackpotImpactStrength impactStrength)
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(impactStrength);
        }
    }
}
