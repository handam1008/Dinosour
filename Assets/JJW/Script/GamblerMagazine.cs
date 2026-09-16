using System;
using System.Collections.Generic;
using UnityEngine;
public enum GamblerCoinType
{
    Normal,
    Roulette
}
public class GamblerMagazine : MonoBehaviour
{
   [SerializeField, Min(1)] private int magazineSize = 3;
    [SerializeField, Min(0f)] private float reloadTime = 1.5f;

    private readonly Queue<GamblerCoinType> magazine = new Queue<GamblerCoinType>();

    private float reloadFinishTime;
    private bool isWaitingForReload;

    public int CurrentAmmo => magazine.Count;
    public int MaxAmmo => magazineSize;
    public bool IsWaitingForReload => isWaitingForReload;

    public event Action<int, int> AmmoChanged;
    public event Action Reloaded;

    private void Awake()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        Reload();
    }

    private void Update()
    {
        TryCompleteReload();
    }

    public bool CanFire()
    {
        TryCompleteReload();
        return magazine.Count > 0;
    }

    public bool TryPeekNext(out GamblerCoinType coinType)
    {
        TryCompleteReload();

        if (magazine.Count == 0)
        {
            coinType = GamblerCoinType.Normal;
            return false;
        }

        coinType = magazine.Peek();
        return true;
    }

    public bool TryTakeNext(out GamblerCoinType coinType)
    {
        TryCompleteReload();

        if (magazine.Count == 0)
        {
            coinType = GamblerCoinType.Normal;
            return false;
        }

        coinType = magazine.Dequeue();

        AmmoChanged?.Invoke(magazine.Count, magazineSize);

        reloadFinishTime = Time.time + reloadTime;
        isWaitingForReload = true;

        return true;
    }

    public GamblerCoinType[] GetRemainingCoins()
    {
        return magazine.ToArray();
    }

    public void Reload()
    {
        magazine.Clear();

        int roulettePosition = UnityEngine.Random.Range(0, magazineSize);

        for (int i = 0; i < magazineSize; i++)
        {
            if (i == roulettePosition)
            {
                magazine.Enqueue(GamblerCoinType.Roulette);
            }
            else
            {
                magazine.Enqueue(GamblerCoinType.Normal);
            }
        }

        isWaitingForReload = false;

        AmmoChanged?.Invoke(magazine.Count, magazineSize);
        Reloaded?.Invoke();
    }

    private void TryCompleteReload()
    {
        if (!isWaitingForReload)
        {
            return;
        }

        if (Time.time < reloadFinishTime)
        {
            return;
        }

        Reload();
    }
}
