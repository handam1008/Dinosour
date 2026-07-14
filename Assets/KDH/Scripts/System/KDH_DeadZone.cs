using UnityEngine;

public class KDH_DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            GameObject bulletRoot = collision.attachedRigidbody.gameObject;
        
            if (!bulletRoot.activeSelf) return;

            bulletRoot.SetActive(false);
            KDH_PoolManager.instance.bullets.Push(bulletRoot);
        }
        else if (collision.CompareTag("Player"))
        {
            collision.gameObject.transform.position = Vector3.zero;
        }
    }
}
