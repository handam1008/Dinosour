using KDH.Scripts.Objects;
using UnityEngine;

public class KDH_WaterfallEffect : MonoBehaviour
{
    [SerializeField] private KDH_Waterfall waterfall;
    ParticleSystem _particles;

    void Awake()
    {
        _particles = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (waterfall.CanWaterfall)
        {
            if (!_particles.isPlaying)
            {
                _particles.Play();
            }
        }
        else
        {
            _particles.Stop();
        }
    }
}
