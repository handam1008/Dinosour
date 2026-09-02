using System.Collections;
using KDH.Scripts.Bullet;
using KDH.Scripts.Upgrade;
using SSW;
using UnityEngine;
using UnityEngine.Events;

public class KDH_FireBullet : KDH_AbstractBulletAbility
{
    [SerializeField] private int dotDamage = 5;
    [SerializeField] private int dotDamageCount = 6;
    [field: SerializeField] public float FireireDuration { get; private set; }= 3f;

    [SerializeField] private UnityEvent onHitPlayer;
        
    [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        
    public Transform HitPoint { get; private set; }

    public KDH_Bullet Bullet { get; private set; }
        
    public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
    {
            if (bullet.IsUpgraded)
            {
                StartCoroutine(TakeDamageDelay(collision, dotDamage * bullet.UpgradValue, FireireDuration * bullet.UpgradValue));
                HitPoint = collision.transform;
                Bullet = bullet;
                    
                onHitPlayer?.Invoke();
                return;
            }
                
            StartCoroutine(TakeDamageDelay(collision, 1, FireireDuration));
            HitPoint = collision.transform;
            Bullet = bullet;
                
            onHitPlayer?.Invoke();
    }

    private IEnumerator TakeDamageDelay(Collider2D collision, float damage, float duration)
    {
        for (int i = 0; i < dotDamageCount; i++)
        {
            yield return new WaitForSeconds(dotDamageCount / FireireDuration);
            if (collision.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}
