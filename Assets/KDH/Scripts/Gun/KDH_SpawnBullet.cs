using System.Collections.Generic;
using UnityEngine;

namespace KDH.Scripts.Gun
{
    public class KDH_SpawnBullet : MonoBehaviour
    {
        [field: SerializeField] public int bulletCount { get; private set; } = 10;
        public Stack<GameObject> bullets = new Stack<GameObject>();
        
        public void CreateBullet(GameObject bulletPrefab, KDH_Gun gun)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab, gun.GunPos.position, transform.rotation);
                bullet.SetActive(false);
                bullets.Push(bullet);
            }
        }
    }
}
