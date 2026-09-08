using System.Collections.Generic;
using UnityEngine;

public class GamblerCoinPool : MonoBehaviour
{
       [Header("Prefabs")]
    [SerializeField] private GamblerCoinProjectile normalCoinPrefab;
    [SerializeField] private GamblerCoinProjectile rouletteCoinPrefab;

    [Header("Pool Settings")]
    [SerializeField, Min(0)] private int normalInitialSize = 10;
    [SerializeField, Min(0)] private int rouletteInitialSize = 3;
    [SerializeField] private Transform poolRoot;

    private readonly Queue<GamblerCoinProjectile> normalCoins = new Queue<GamblerCoinProjectile>();
    private readonly Queue<GamblerCoinProjectile> rouletteCoins = new Queue<GamblerCoinProjectile>();

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        FillPool(normalCoinPrefab, normalInitialSize, GamblerCoinType.Normal, normalCoins);
        FillPool(rouletteCoinPrefab, rouletteInitialSize, GamblerCoinType.Roulette, rouletteCoins);
    }

    public GamblerCoinProjectile Get(GamblerCoinType coinType, Vector3 position, Quaternion rotation)
    {
        Queue<GamblerCoinProjectile> selectedPool = GetPool(coinType);
        GamblerCoinProjectile selectedPrefab = GetPrefab(coinType);

        if (selectedPrefab == null)
        {
            return null;
        }

        GamblerCoinProjectile coin;

        if (selectedPool.Count > 0)
        {
            coin = selectedPool.Dequeue();
        }
        else
        {
            coin = CreateCoin(selectedPrefab, coinType);
        }

        if (coin == null)
        {
            return null;
        }

        coin.SetCoinType(coinType);
        coin.transform.SetParent(null, true);
        coin.transform.SetPositionAndRotation(position, rotation);
        coin.gameObject.SetActive(true);

        return coin;
    }

    public void Release(GamblerCoinProjectile coin)
    {
        if (coin == null || !coin.gameObject.activeSelf)
        {
            return;
        }

        coin.gameObject.SetActive(false);
        coin.transform.SetParent(poolRoot, false);

        Queue<GamblerCoinProjectile> selectedPool = GetPool(coin.CoinType);
        selectedPool.Enqueue(coin);
    }

    private void FillPool(GamblerCoinProjectile prefab, int amount, GamblerCoinType coinType, Queue<GamblerCoinProjectile> targetPool)
    {
        if (prefab == null)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            GamblerCoinProjectile coin = CreateCoin(prefab, coinType);

            if (coin != null)
            {
                targetPool.Enqueue(coin);
            }
        }
    }

    private GamblerCoinProjectile CreateCoin(GamblerCoinProjectile prefab, GamblerCoinType coinType)
    {
        if (prefab == null)
        {
            return null;
        }

        GamblerCoinProjectile coin = Instantiate(prefab, poolRoot);
        coin.SetCoinType(coinType);
        coin.gameObject.SetActive(false);

        return coin;
    }

    private Queue<GamblerCoinProjectile> GetPool(GamblerCoinType coinType)
    {
        if (coinType == GamblerCoinType.Roulette)
        {
            return rouletteCoins;
        }

        return normalCoins;
    }

    private GamblerCoinProjectile GetPrefab(GamblerCoinType coinType)
    {
        if (coinType == GamblerCoinType.Roulette)
        {
            return rouletteCoinPrefab;
        }

        return normalCoinPrefab;
    }
}
