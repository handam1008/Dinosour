using System;
using UnityEngine;

public class KDH_Shoot : MonoBehaviour
{
    [SerializeField] private GameObject _bullet;

    public Action PlayerShoot;

    private void Start()
    {
        PlayerShoot += Shoot;
    }

    private void Shoot()
    {
        _bullet.SetActive(true);
    }
}
