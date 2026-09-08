using System;
using UnityEngine;

public class GamblerCoinShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GamblerMouseAim mouseAim;
    [SerializeField] private GamblerCoinPool coinPool;
    [SerializeField] private GamblerMagazine magazine;
    [SerializeField] private Transform owner;

    [Header("Fire Settings")]
    [SerializeField, Min(0f)] private float fireCooldown = 0.2f;
    [SerializeField, Min(0f)] private float spawnOffset = 0.15f;

    private float nextFireTime;

    public event Action<GamblerCoinProjectile> CoinFired;
    public event Action RouletteCoinFired;

    private void Awake()
    {
        if (mouseAim == null)
        {
            mouseAim = GetComponent<GamblerMouseAim>();
        }

        if (coinPool == null)
        {
            coinPool = GetComponent<GamblerCoinPool>();
        }

        if (magazine == null)
        {
            magazine = GetComponent<GamblerMagazine>();
        }

        if (owner == null)
        {
            if (transform.parent != null)
            {
                owner = transform.parent;
            }
            else
            {
                owner = transform;
            }
        }
    }

    public bool TryFire()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (mouseAim == null || coinPool == null || magazine == null)
        {
            return false;
        }

        if (!magazine.TryPeekNext(out GamblerCoinType nextCoinType))
        {
            return false;
        }

        if (!mouseAim.TryGetFireDirection(out Vector2 direction))
        {
            return false;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnOffset);

        GamblerCoinProjectile coin = coinPool.Get(
            nextCoinType,
            spawnPosition,
            Quaternion.Euler(0f, 0f, angle - 90f)
        );

        if (coin == null)
        {
            return false;
        }

        if (!magazine.TryTakeNext(out GamblerCoinType firedCoinType))
        {
            coinPool.Release(coin);
            return false;
        }

        coin.Initialize(direction, owner, coinPool.Release);

        nextFireTime = Time.time + fireCooldown;

        CoinFired?.Invoke(coin);

        if (firedCoinType == GamblerCoinType.Roulette)
        {
            RouletteCoinFired?.Invoke();
        }

        return true;
    }
}
