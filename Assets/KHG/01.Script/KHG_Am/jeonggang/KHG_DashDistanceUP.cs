using SSW;
using UnityEngine;

public class KHG_DashDistanceUP : MonoBehaviour
{
    [Header("대쉬 후 이동속도 증가")]
    [SerializeField] private float speedPercent = 0.5f;
    [SerializeField] private float duration = 3f;

    [Header("대쉬 거리 증가")]
    [SerializeField] private float dashDistance = 0.5f;

    private ISpeedable targetSpeedable;
    private KHG_Dash dash;

    private bool wasDashing = false;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();

        targetSpeedable = GetComponent<ISpeedable>();

        if (dash == null)
        {
            Debug.LogError("KHG_Dash를 찾을 수 없습니다.");
        }

        if (targetSpeedable == null)
        {
            Debug.LogError("ISpeedable을 찾을 수 없습니다.");
        }
    }

    private void Start()
    {
        if (dash != null)
        {
            dash._dashDuration *= (1f + dashDistance);

            Debug.Log(
                "대쉬 거리 증가 적용! 현재 대쉬 시간: " +
                dash._dashDuration
            );
        }
    }

    private void Update()
    {
        if (dash == null)
            return;

        // 대쉬 시작 감지
        if (dash.IsDashing)
        {
            wasDashing = true;
        }

        // 대쉬가 끝난 순간
        if (wasDashing && !dash.IsDashing)
        {
            wasDashing = false;

            ActivateSpeedBoost();
        }
    }

    public void ActivateSpeedBoost()
    {
        if (targetSpeedable == null)
            return;

        targetSpeedable.ApplySpeed(
            speedPercent,
            duration
        );

        Debug.Log(
            "대쉬 후 이동속도 증가! +" +
            (speedPercent * 100f) +
            "% / " +
            duration +
            "초"
        );
    }

    public void ApplySpeed(float amount, float duration)
    {
        targetSpeedable?.ApplySpeed(amount, duration);
    }
}