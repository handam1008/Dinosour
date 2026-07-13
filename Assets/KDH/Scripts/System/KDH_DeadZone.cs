using UnityEngine;

public class KDH_DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            collision.gameObject.SetActive(false);
            KDH_PoolManager.instance.bullets.Push(collision.gameObject);
        }
        else if (collision.CompareTag("Player"))
        {
            collision.gameObject.transform.position = Vector3.zero;
        }
    }
}
