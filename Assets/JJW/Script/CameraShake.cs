using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    [Header("Impulse Sources")]
    [SerializeField]
    private CinemachineImpulseSource mediumImpulseSource;

    [SerializeField]
    private CinemachineImpulseSource strongImpulseSource;

    [Header("Force")]
    [Min(0f)]
    [SerializeField] private float mediumForce = 1f;

    [Min(0f)]
    [SerializeField] private float strongForce = 1.2f;

    public void Shake(JackpotImpactStrength impactStrength)
    {
        if (impactStrength == JackpotImpactStrength.Strong)
        {
            PlayStrongShake();
            return;
        }

        PlayMediumShake();
    }

    private void PlayMediumShake()
    {
        if (mediumImpulseSource == null)
        {
            Debug.LogError("중간 Impulse Source가 연결되지 않았습니다.", this);

            return;
        }

        mediumImpulseSource.GenerateImpulseWithForce(
            mediumForce);
    }

    private void PlayStrongShake()
    {
        if (strongImpulseSource == null)
        {
            Debug.LogError("강한 Impulse Source가 연결되지 않았습니다.", this);

            return;
        }

        strongImpulseSource.GenerateImpulseWithForce(
            strongForce);
    }
}
