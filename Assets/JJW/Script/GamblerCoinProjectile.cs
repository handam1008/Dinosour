using System;
using SSW;
using UnityEngine;

public class GamblerCoinProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;

    [SerializeField, Min(0f)] private float speed = 15f;
    [SerializeField, Min(0.01f)] private float lifetime = 3f;
    [SerializeField, Min(0f)] private float ownerHitDelay = 0.1f;
    [SerializeField] private float coinDamage = 10f;

    private Action<GamblerCoinProjectile> releaseToPool;
    private Transform owner;
    private float releaseTime;
    private float ownerHitEnableTime;
    private bool isFlying;

    public float CoinDamage => coinDamage;
    public GamblerCoinType CoinType { get; private set; }

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (isFlying && Time.time >= releaseTime)
        {
            ReturnToPool();
        }
    }

    public void SetCoinType(GamblerCoinType coinType)
    {
        CoinType = coinType;
    }

    public void Initialize(Vector2 direction, Transform shooterOwner, Action<GamblerCoinProjectile> releaseAction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            releaseAction?.Invoke(this);
            return;
        }

        owner = shooterOwner;
        releaseToPool = releaseAction;
        releaseTime = Time.time + lifetime;
        ownerHitEnableTime = Time.time + ownerHitDelay;
        isFlying = true;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = direction.normalized * speed;
#else
        body.velocity = direction.normalized * speed;
#endif
    }

    private bool IsOwner(Collider2D other)
    {
        if (owner == null)
        {
            return false;
        }

        return other.transform == owner || other.transform.IsChildOf(owner);
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

        Action<GamblerCoinProjectile> releaseAction = releaseToPool;

        releaseToPool = null;
        owner = null;

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
        if (IsOwner(other) && Time.time < ownerHitEnableTime)
        {
            return;
        }

        if (other.TryGetComponent<IDamageable>(out var damage))
        {
            damage.TakeDamage(CoinDamage);
            ReturnToPool();
            return;
        }

        if (other.CompareTag("Ground"))
        {
            ReturnToPool();
        }
    }
}
