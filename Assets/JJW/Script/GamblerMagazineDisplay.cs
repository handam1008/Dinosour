using UnityEngine;

public class GamblerMagazineDisplay : MonoBehaviour
{
    [SerializeField] private GamblerMagazine magazine;
    [SerializeField] private SpriteRenderer[] coinSlots;
    [SerializeField] private Sprite normalCoinSprite;
    [SerializeField] private Sprite rouletteCoinSprite;

    private void Awake()
    {
        if (magazine == null)
        {
            magazine = GetComponentInParent<GamblerMagazine>();
        }
    }

    private void OnEnable()
    {
        if (magazine != null)
        {
            magazine.AmmoChanged += HandleAmmoChanged;
        }
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (magazine != null)
        {
            magazine.AmmoChanged -= HandleAmmoChanged;
        }
    }

    private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (magazine == null)
        {
            return;
        }

        GamblerCoinType[] remainingCoins = magazine.GetRemainingCoins();

        for (int i = 0; i < coinSlots.Length; i++)
        {
            if (coinSlots[i] != null)
            {
                coinSlots[i].gameObject.SetActive(false);
            }
        }

        int displayCount = Mathf.Min(remainingCoins.Length, coinSlots.Length);

        for (int i = 0; i < displayCount; i++)
        {
            int slotIndex = displayCount - 1 - i;
            SpriteRenderer coinSlot = coinSlots[slotIndex];

            if (coinSlot == null)
            {
                continue;
            }

            coinSlot.gameObject.SetActive(true);

            if (remainingCoins[i] == GamblerCoinType.Roulette)
            {
                coinSlot.sprite = rouletteCoinSprite;
            }
            else
            {
                coinSlot.sprite = normalCoinSprite;
            }
        }
    }
}
