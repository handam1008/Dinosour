using UnityEngine;

public class GamblerCoinProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D body;

    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float speed = 15f;
    [Min(0.01f)]
    [SerializeField] private float lifetime = 3f;

    [Header("Collision")]
    [SerializeField] private LayerMask destroyOnLayers;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    public void Initialize(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogError("GamblerCoinProjectile received an empty direction.", this);
            Destroy(gameObject);
            return;
        }

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = direction.normalized * speed;
#else
        body.velocity = direction.normalized * speed;
#endif

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDestroyOnLayer(other.gameObject.layer);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDestroyOnLayer(collision.gameObject.layer);
    }

    private void TryDestroyOnLayer(int otherLayer)
    {
        int otherLayerMask = 1 << otherLayer;
        if ((destroyOnLayers.value & otherLayerMask) != 0)
        {
            Destroy(gameObject);
        }
    }
}
