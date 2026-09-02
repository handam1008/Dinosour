using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (impulseSource == null)
        {
            impulseSource =
                GetComponent<CinemachineImpulseSource>();
        }
    }

    public void Shake()
    {
        if (impulseSource == null)
        {
            Debug.LogError("시네머신 Impulse Source 연결안됨", this);

            return;
        }

        impulseSource.GenerateImpulse();
    }
}
