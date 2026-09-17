using UnityEngine;

public class KHG_CameraShake : MonoBehaviour
{
    //데미지 받는코드에 KHG_CameraShake.Play();이거 넣으면 됨
    public static KHG_CameraShake Instance { get; private set; }

    [SerializeField, Min(0f)] private float defaultAmount = 0.1f;

    private float shakeTime;
    private float shakeAmount;
    private Vector3 originalLocalPosition;
    private bool isShaking;

    private void Awake()
    {
        Instance = this;
        originalLocalPosition = transform.localPosition;
    }

    public static void Play(float time = 0.12f, float amount = 0.08f)
    {
        if (Instance == null)
        {
            Debug.Log("Main Camera에 KHG_CameraShake가 없습니다.");
            return;
        }

        Instance.VibrateForTime(time, amount);
    }

    private void VibrateForTime(float time, float amount)
    {
        if (time <= 0f)
            return;

        if (!isShaking)
            originalLocalPosition = transform.localPosition;

        isShaking = true;
        shakeTime = Mathf.Max(shakeTime, time);
        shakeAmount = Mathf.Max(shakeAmount, amount);
    }

    private void LateUpdate()
    {
        if (!isShaking)
            return;

        shakeTime -= Time.deltaTime;

        if (shakeTime <= 0f)
        {
            transform.localPosition = originalLocalPosition;
            shakeTime = 0f;
            shakeAmount = 0f;
            isShaking = false;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * shakeAmount;
        transform.localPosition =
            originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
    }
}