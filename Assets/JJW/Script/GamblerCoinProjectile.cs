using System;
using SSW;
using UnityEngine;

public class GamblerCoinProjectile : MonoBehaviour
{
  
    [SerializeField]
    private Rigidbody2D body;
    
    [Min(0f)]
    [SerializeField] private float speed = 15f;
    [Min(0.01f)] [SerializeField] private float lifetime = 3f;
    private float _coinDamage = 10f;
    public float CoinDamage {get => _coinDamage; private set => _coinDamage = value;}

    private Action<GamblerCoinProjectile> releaseToPool;

    private float releaseTime;
    private bool isFlying;

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

    private void Update()
    {
        if (isFlying && Time.time >= releaseTime)
        {
            ReturnToPool();
        }
    }

    public void Initialize(Vector2 direction, Action<GamblerCoinProjectile> releaseAction)
        
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogError("GamblerCoinProjectile received an empty direction.",this);

            if (releaseAction != null)
            {
                releaseAction.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }

            return;
        }

        releaseToPool = releaseAction;
        releaseTime = Time.time + lifetime;
        isFlying = true;

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = direction.normalized * speed;
#else
        body.velocity = direction.normalized * speed;
#endif
    }

    private void ReturnToPool()
    {
        if (!isFlying)
        {
            return;
        }

        isFlying = false;

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = Vector2.zero;
#else
        body.velocity = Vector2.zero;
#endif

        Action<GamblerCoinProjectile>releaseAction = releaseToPool;

        releaseToPool = null;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var damage))
        {
            damage.TakeDamage(CoinDamage);
            ReturnToPool();
        }

        if (other.CompareTag("Ground"))
        {
            ReturnToPool();
        }
    }
}
