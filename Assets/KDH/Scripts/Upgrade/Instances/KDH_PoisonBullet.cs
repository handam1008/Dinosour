using System.Collections;
using System.Collections.Generic;
using KDH.Scripts.Bullet;
using KDH.Scripts.Upgrade;
using SSW;
using UnityEngine;
using UnityEngine.Events;

public class KDH_PoisonBullet : KDH_AbstractBulletAbility
{
    [SerializeField] private float dotDamage = 7f;
    [field: SerializeField] public int DotCount { get; private set; }= 7;
    [field: SerializeField] public float PoisonDuration { get; private set; }= 4f;
    
    [SerializeField] private UnityEvent onHitPlayer;
    
    [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
    
    public Transform HitPoint { get; private set; }

    public KDH_Bullet Bullet { get; private set; }

    public override void BulletAbility(Collider2D collision,  KDH_Bullet bullet)
    {
        if (bullet.IsUpgraded)
        {
            StartCoroutine(DotDamage(collision, dotDamage * bullet.UpgradValue));
            HitPoint = collision.transform;
            Bullet = bullet;
            
            onHitPlayer?.Invoke();
            return;
        }
        
        StartCoroutine(DotDamage(collision, dotDamage));
        HitPoint = collision.transform;
        Bullet = bullet;
            
        onHitPlayer?.Invoke();
    }

    private IEnumerator DotDamage(Collider2D collision, float damage)
    {
        for (int i = 0; i < DotCount; i++)
        {
            yield return new WaitForSeconds(PoisonDuration / DotCount);
            collision.TryGetComponent(out IDamageable damageable);
            damageable?.TakeDamage(damage);
        }
    }
}
