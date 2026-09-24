using UnityEngine;

public class NuclearBlast : MonoBehaviour
{
    const float AuthoredRadius = 2.5f;

    [SerializeField] ParticleSystem[] _debris;
    [SerializeField] NuclearWave _wave;

    public void Play(float radius, LayerMask walls)
    {
        transform.localScale = Vector3.one * (radius / AuthoredRadius);

        foreach (ParticleSystem system in _debris)
        {
            ParticleSystem.CollisionModule collision = system.collision;
            collision.collidesWith = walls;
        }

        _wave.Play(radius, walls);
    }
}
