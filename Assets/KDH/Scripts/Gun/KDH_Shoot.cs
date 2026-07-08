using System;
using UnityEngine;

public class KDH_Shoot : MonoBehaviour
{
    public Action PlayerShoot;

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
        GameObject bullet = KDH_PoolManager.instance.bullets.Pop();
        bullet.transform.position = KDH_GameManager.instanec.player.GetComponentInChildren<KDH_Gun>().gunPos.position;
        bullet.SetActive(true);
    }
}
