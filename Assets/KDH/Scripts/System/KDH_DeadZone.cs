using KDH.Scripts.Gun;
using UnityEngine;

namespace KDH.Scripts.System
{
    public class KDH_DeadZone : MonoBehaviour
    {
        [SerializeField] private KDH_SpawnBullet spawnBullet;
    
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Bullet"))
            {
                GameObject bulletRoot = collision.attachedRigidbody.gameObject;
        
                if (!bulletRoot.activeSelf) return;

                bulletRoot.SetActive(false);
                spawnBullet.bullets.Push(bulletRoot);
            }
            else if (collision.CompareTag("Player"))
            {
                collision.gameObject.transform.position = Vector3.zero;
            }
        }
    }
}
