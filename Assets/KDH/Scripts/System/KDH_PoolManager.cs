using System.Collections.Generic;
using UnityEngine;

public class KDH_PoolManager : MonoBehaviour
{
    public static KDH_PoolManager instance;

    [SerializeField] private GameObject bulletPrefab;
    public Stack<GameObject> bullets = new Stack<GameObject>();
    private int bulletCount = 10;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        CreateBullet();
    }

    private void CreateBullet()
    {
        for (int i = 0; i < bulletCount; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bullets.Push(bullet);
        }
    }
}
