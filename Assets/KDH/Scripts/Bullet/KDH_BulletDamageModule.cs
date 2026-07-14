using SSW;
using UnityEngine;

public class KDH_BulletDamageModule : MonoBehaviour
{
    [SerializeField] private float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var dmg))
            dmg.TakeDamage(damage);
    }
}
