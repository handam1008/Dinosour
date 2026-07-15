using System.Collections.Generic;
using UnityEngine;

public class KDH_SpawnBullet : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    private KDH_Gun _gun;
    public Stack<GameObject> bullets = new Stack<GameObject>();
    private int bulletCount = 10;

    private void Awake() => _gun = GetComponent<KDH_Gun>();
    
    private void Start()
    {
        CreateBullet();
    }

    private void CreateBullet()
    {
        for (int i = 0; i < bulletCount; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, _gun.gunPos.position, transform.rotation);
            bullet.SetActive(false);
            bullets.Push(bullet);
        }
    }
}
