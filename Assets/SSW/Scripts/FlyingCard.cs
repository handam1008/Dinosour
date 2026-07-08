using UnityEngine;

public class FlyingCard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject);
    }
}
