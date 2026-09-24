using UnityEngine;

public class NuclearCharge : MonoBehaviour
{
    const float AuthoredDuration = 1f;

    [SerializeField] ParticleSystem[] _systems;

    public void Play(float duration)
    {
        foreach (ParticleSystem system in _systems)
        {
            ParticleSystem.MainModule main = system.main;
            main.simulationSpeed = AuthoredDuration / duration;
        }
    }
}
