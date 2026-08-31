using System;
using UnityEngine;

public class GamblerCoinShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GamblerMouseAim mouseAim;

    [SerializeField]
    private GamblerCoinPool coinPool;

    [Header("Fire Settings")]
    [Min(0f)]
    [SerializeField]
    private float fireCooldown = 0.2f;

    private float nextFireTime;

    public event Action<GamblerCoinProjectile>
        CoinFired;

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
    }

    public bool TryFire()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (mouseAim == null ||
            coinPool == null)
        {
            Debug.LogError("GamblerCoinShooter references are not fully assigned.", this);

            return false;
        }

        if (!mouseAim.TryGetFireDirection(out Vector2 direction))
        {
            return false;
        }

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GamblerCoinProjectile coin = coinPool.Get(transform.position, Quaternion.Euler(0f, 0f, angle-90));

        if (coin == null)
        {
            return false;
        }

        coin.Initialize(direction, coinPool.Release);

        nextFireTime = Time.time + fireCooldown;

        CoinFired?.Invoke(coin);

        return true;
    }
}
