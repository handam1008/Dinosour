using System.Collections.Generic;
using UnityEngine;

public class GamblerCoinPool : MonoBehaviour
{
        [Header("Pool Settings")]
        [SerializeField] private GamblerCoinProjectile coinPrefab;
        [Min(0)]
        [SerializeField] private int initialSize = 10;
        [SerializeField] private Transform poolRoot;
    
        private readonly Queue<GamblerCoinProjectile> availableCoins =
            new Queue<GamblerCoinProjectile>();
    
        private void Awake()
        {
            if (poolRoot == null)
            {
                poolRoot = transform;
            }
    
            if (coinPrefab == null)
            {
                Debug.LogError("GamblerCoinPool needs a Coin Prefab reference.", this);
                return;
            }
    
            for (int i = 0; i < initialSize; i++)
            {
                GamblerCoinProjectile coin = CreateCoin();
                availableCoins.Enqueue(coin);
            }
        }
    
        public GamblerCoinProjectile Get(Vector3 position, Quaternion rotation)
        {
            if (coinPrefab == null)
            {
                return null;
            }
    
            GamblerCoinProjectile coin = availableCoins.Count > 0
                ? availableCoins.Dequeue()
                : CreateCoin();
    
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
            availableCoins.Enqueue(coin);
        }
    
        private GamblerCoinProjectile CreateCoin()
        {
            GamblerCoinProjectile coin = Instantiate(coinPrefab, poolRoot);
            coin.gameObject.SetActive(false);
            return coin;
        }
}
