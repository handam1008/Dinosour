using SSW;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_DamageCaster : MonoBehaviour
    {
        [SerializeField] private float damage;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(damage);
        }
    }
}
