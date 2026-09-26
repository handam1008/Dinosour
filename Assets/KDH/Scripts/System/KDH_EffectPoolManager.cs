using System.Collections.Generic;
using UnityEngine;

namespace KDH.Scripts.System
{
    public class KDH_EffectPoolManager : MonoBehaviour
    {
        public static KDH_EffectPoolManager Instance { get; private set; }

        [SerializeField] private KDH_EffectPoolList effectPoolList;
        [SerializeField] private int spawnCount = 15;

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

        private void Awake()
        {
            Instance = this;
            CreateEffects();
        }

        private void CreateEffects()
        {
            foreach (var data in effectPoolList.list)
            {
                CreatePool(data.bulletNormalEffectPrefab);
                CreatePool(data.bulletUpgradedEffectPrefab);
                CreatePool(data.bulletTrailEffectPrefab);
            }
        }

        private void CreatePool(GameObject prefab)
        {
            if (prefab == null || _pools.ContainsKey(prefab))
                return;

            var pool = new Queue<GameObject>();
            _pools.Add(prefab, pool);

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject effect = Instantiate(prefab);
                effect.SetActive(false);
                pool.Enqueue(effect);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("Effect prefab이 비어 있습니다.");
                return null;
            }

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                CreatePool(prefab);
                pool = _pools[prefab];
            }

            GameObject effect = pool.Count > 0
                ? pool.Dequeue()
                : Instantiate(prefab, transform);

            effect.transform.SetPositionAndRotation(position, rotation);
            effect.SetActive(true);

            return effect;
        }

        public void Release(GameObject prefab, GameObject effect)
        {
            effect.SetActive(false);
            effect.transform.SetParent(transform);

            _pools[prefab].Enqueue(effect);
        }
    }
}