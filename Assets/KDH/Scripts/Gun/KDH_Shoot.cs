using System;
using System.Collections.Generic;
using UnityEngine;

public class KDH_Shoot : MonoBehaviour
{
    private KDH_SpawnBullet SpawnBullet;
    private KDH_Gun gun;
    public Action PlayerShoot;

    private void Awake()
    {
        SpawnBullet = GetComponentInChildren<KDH_SpawnBullet>();
        gun = GetComponentInChildren<KDH_Gun>();
    }
    
    private void Start()
    {
        PlayerShoot += Shoot;
    }

    private void OnAttack()
    {
        PlayerShoot?.Invoke();
    }

    private void Shoot()
    {
        if (SpawnBullet.bullets.Count <= 0) return;
        
        GameObject bullet = SpawnBullet.bullets.Pop();
        bullet.transform.position = gun.gunPos.position;
        
        KDH_Bullet bulletScript = bullet.GetComponent<KDH_Bullet>();
        bulletScript.Init(gun.GetFireDirection());
        
        bullet.SetActive(true);
    }
}
