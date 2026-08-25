using System;
using UnityEngine;

public class GamblerCoinShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GamblerMouseAim mouseAim;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GamblerCoinProjectile coinPrefab;

    [Header("Fire Settings")]
    [Min(0f)]
    [SerializeField] private float fireCooldown = 0.2f;

    private float nextFireTime;

    // Sound, VFX, magazine UI, and roulette can subscribe later.
    public event Action<GamblerCoinProjectile> CoinFired;

    public bool TryFire()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (mouseAim == null || muzzle == null || coinPrefab == null)
        {
            Debug.LogError("GamblerCoinShooter references are not fully assigned.", this);
            return false;
        }

        if (!mouseAim.TryGetDirectionFrom(muzzle.position, out Vector2 direction))
        {
            return false;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GamblerCoinProjectile coin = Instantiate(
            coinPrefab,
            muzzle.position,
            Quaternion.Euler(0f, 0f, angle));

        coin.Initialize(direction);

        nextFireTime = Time.time + fireCooldown;
        CoinFired?.Invoke(coin);
        return true;
    }
}
