using System;
using System.Collections.Generic;
using JJW.Script.Augments;
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
    [SerializeField] private GamblerAugmentController augmentController;

    private readonly Queue<GamblerCoinType> magazine =
        new Queue<GamblerCoinType>();

    private float reloadFinishTime;
    private bool isWaitingForReload;
    private bool lastMoreChancesState;
    private bool moreChancesStateInitialized;
    private bool isSubscribed;

    public int CurrentAmmo => magazine.Count;
    public int MaxAmmo => magazineSize;
    public bool IsWaitingForReload => isWaitingForReload;

    public event Action<int, int> AmmoChanged;
    public event Action Reloaded;

    private void Awake()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        FindAugmentController();
        Reload();
    }

    private void OnEnable()
    {
        FindAugmentController();
        SubscribeAugmentEvent();
    }

    private void Start()
    {
        SynchronizeMoreChances(true);
    }

    private void Update()
    {
        if (augmentController == null)
        {
            FindAugmentController();
            SubscribeAugmentEvent();
        }

        SynchronizeMoreChances(false);
        TryCompleteReload();
    }

    private void OnDisable()
    {
        UnsubscribeAugmentEvent();
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

        AmmoChanged?.Invoke(
            magazine.Count,
            magazineSize);

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

        bool hasMoreChances = HasMoreChances();

        if (hasMoreChances)
        {
            FillWithRouletteCoins();
        }
        else
        {
            FillNormalMagazine();
        }

        lastMoreChancesState = hasMoreChances;
        moreChancesStateInitialized = true;
        isWaitingForReload = false;

        AmmoChanged?.Invoke(
            magazine.Count,
            magazineSize);

        Reloaded?.Invoke();
    }

    private void FillWithRouletteCoins()
    {
        for (int i = 0; i < magazineSize; i++)
        {
            magazine.Enqueue(
                GamblerCoinType.Roulette);
        }
    }

    private void FillNormalMagazine()
    {
        int roulettePosition =
            UnityEngine.Random.Range(
                0,
                magazineSize);

        for (int i = 0; i < magazineSize; i++)
        {
            GamblerCoinType coinType =
                i == roulettePosition
                    ? GamblerCoinType.Roulette
                    : GamblerCoinType.Normal;

            magazine.Enqueue(coinType);
        }
    }

    private void SynchronizeMoreChances(
        bool forceReload)
    {
        bool currentState = HasMoreChances();

        if (!moreChancesStateInitialized)
        {
            lastMoreChancesState = currentState;
            moreChancesStateInitialized = true;

            if (forceReload)
            {
                Reload();
            }

            return;
        }

        if (!forceReload
            && currentState == lastMoreChancesState)
        {
            return;
        }

        lastMoreChancesState = currentState;



        Reload();
    }

    private bool HasMoreChances()
    {
        return augmentController != null && augmentController.Has(GamblerAugmentType.MoreChances);
    }

    private void OnAugmentAcquired(
        GamblerAugmentType type)
    {
        if (type != GamblerAugmentType.MoreChances)
        {
            return;
        }

       

        lastMoreChancesState = true;
        moreChancesStateInitialized = true;
        Reload();
    }

    private void FindAugmentController()
    {
        if (augmentController != null)
        {
            return;
        }

        augmentController =
            GetComponentInParent<GamblerAugmentController>();
    }

    private void SubscribeAugmentEvent()
    {
        if (augmentController == null || isSubscribed)
        {
            return;
        }

        augmentController.AugmentAcquired += OnAugmentAcquired;

        isSubscribed = true;
    }

    private void UnsubscribeAugmentEvent()
    {
        if (augmentController == null || !isSubscribed)
        {
            return;
        }

        augmentController.AugmentAcquired -= OnAugmentAcquired;

        isSubscribed = false;
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