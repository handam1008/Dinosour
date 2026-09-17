using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Volcano : MonoBehaviour
    {
        [SerializeField] private float damage;

        void OnParticleCollision(GameObject other)
        {
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}