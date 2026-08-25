using SSW;
using UnityEngine;

public class KHG_DashSpeed : MonoBehaviour, ISpeedable
{
    [Header("대쉬 후 이동속도 증가")]
    [SerializeField] private float speedpercent = 0.5f; // 0.1 = 10% 증가
    [SerializeField] private float duration = 3f;             

    private ISpeedable targetSpeedable;

    private void Awake()
    {
        targetSpeedable = GetComponent<ISpeedable>();
    }

    public void ActivateSpeedBoost()
    {
        ApplySpeed(speedpercent, duration);
    }

    public void ApplySpeed(float amount, float duration)
    {
        targetSpeedable?.ApplySpeed(amount, duration);
    }
}