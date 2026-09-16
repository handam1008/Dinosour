using System;
using JJW.Script.Augments;
using SSW;
using UnityEngine;

public class GamblerCoinProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float speed = 15f;
    [SerializeField, Min(0.01f)] private float lifetime = 3f;
    [SerializeField, Min(0f)] private float ownerHitDelay = 0.1f;

    private float coinDamage;
    private Action<GamblerCoinProjectile> releaseToPool;
    private Transform owner;
    private Component damageSource;
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

    public void Init(float damage, float multiplier)
    {
        coinDamage = damage * multiplier;
    }

    public void SetCoinType(GamblerCoinType coinType)
    {
        CoinType = coinType;
    }

    public void Initialize(
        Vector2 direction,
        Transform shooterOwner,
        Component source,
        Action<GamblerCoinProjectile> releaseAction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            releaseAction?.Invoke(this);
            return;
        }

        owner = shooterOwner;
        damageSource = source;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwner(other)
            && Time.time < ownerHitEnableTime)
        {
            return;
        }

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            Component source =
                damageSource != null
                    ? damageSource
                    : this;

            DamageResult result = CombatDamage.Deal(
                source,
                damageable,
                CoinDamage,
                DamageTag.BasicAttack
                | DamageTag.Projectile);

            ApplyCoinUpgradeEffects(
                damageable as Component,
                result);

            ReturnToPool();
            return;
        }

        if (other.CompareTag("Ground"))
        {
            ReturnToPool();
        }
    }

    private void ApplyCoinUpgradeEffects(
        Component target,
        DamageResult result)
    {
        if (damageSource == null)
        {
            return;
        }

        IGamblerCoinHitEffect hitEffect =
            damageSource
                .GetComponentInParent<IGamblerCoinHitEffect>();

        hitEffect?.ApplyCoinHitEffects(
            target,
            result);
    }

    private bool IsOwner(Collider2D other)
    {
        if (owner == null)
        {
            return false;
        }

        return other.transform == owner
            || other.transform.IsChildOf(owner);
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

        Action<GamblerCoinProjectile> releaseAction =
            releaseToPool;

        releaseToPool = null;
        owner = null;
        damageSource = null;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}