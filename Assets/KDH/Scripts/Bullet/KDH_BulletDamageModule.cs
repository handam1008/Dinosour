using UnityEngine;

public class KDH_BulletDamageModule : MonoBehaviour
{
    [SerializeField] private float damage;
    
    private void OnTrigerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var dmg))
            dmg.TakeDamage(damage);
        
        Debug.Log($"닿음: {collision.name}");
    }
}
