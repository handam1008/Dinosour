using System.Collections;
using System.Collections.Generic;
using RYU._01.Script.Argument;
using RYU._01.Script.Potions;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class RandomPotion : MonoBehaviour
{
    [SerializeField] private List<AbstractPotion> potions = new List<AbstractPotion>();
    [SerializeField] private Transform Hand;
    [SerializeField] private Transform PlayerUp;
    [SerializeField] private float potionSpeed = 15f;
    [SerializeField] private float Angle = 15f;
    [SerializeField] private float cycleTime = 1.5f;   // 포션 교체 주기
    [SerializeField] private float spreadAngle = 24f;  // 정밀 조제: 부채꼴 전체 각도

    private WitchAugmentController _augment;

    // 앞(0번)이 손에 든 것, 뒤가 머리 위에서 대기 중인 것
    private readonly List<GameObject> currentPotions = new List<GameObject>();

    // 주머니 증강: 보관 중인 포션과 이번 포션에 이미 교환했는지
    private AbstractPotion _pocket;
    private bool _pocketUsed;

    // UI가 읽어간다
    public AbstractPotion Pocket => _pocket;

    [SerializeField] private bool canCreate = false;
    [SerializeField] private bool canHand = false;

    private void Awake()
    {
        _augment = GetComponentInParent<WitchAugmentController>();
    }

    private void Start()
    {
        canCreate = true;
        canHand = true;
        ResetPocket(); // 라운드가 시작될 때 주머니는 비어 있다
    }

    // 라운드가 끝나면 호출해서 주머니를 비운다
    public void ResetPocket()
    {
        _pocket = null;
        _pocketUsed = false;
    }

    private void Update()
    {
        if (ShiftPressed()) SwapPocket();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentPotions.Count <= 0) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = ((Vector2)mouseWorld - (Vector2)Hand.position).normalized;
            Shoot(dir);
        }

        if (currentPotions.Count > 2)
        {
            GameObject potion = currentPotions[0];
            currentPotions.RemoveAt(0);
            Destroy(potion);
            canHand = true;
        }

        if (canCreate)
        {
            CreatePotion();
            StartCoroutine(WaitCo());
        }

        if (canHand)
        {
            if (currentPotions.Count <= 0) return;

            currentPotions[0].transform.SetParent(Hand, false);
            canHand = false;
        }
    }

    private bool ShiftPressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftShiftKey.wasPressedThisFrame
            || Keyboard.current.rightShiftKey.wasPressedThisFrame;
    }

    // 주머니 증강: 손에 든 포션과 주머니 속 포션을 교환한다
    private void SwapPocket()
    {
        if (_augment == null || !_augment.Has(WitchAugmentType.Pocket)) return;
        if (_pocketUsed) return; // 던지기 전까지 1회만

        AbstractPotion heldData = null;

        // 손에 뭔가 들려 있으면 꺼내서 주머니로 보낼 준비
        if (currentPotions.Count > 0)
        {
            GameObject held = currentPotions[0];
            Potion potion = held.GetComponent<Potion>();
            heldData = potion != null ? potion.Data : null;

            currentPotions.RemoveAt(0);
            Destroy(held);
        }

        // 주머니에 있던 걸 손 자리에 장전한다
        if (_pocket != null)
        {
            GameObject taken = SpawnPotion(_pocket, Hand);
            if (taken != null) currentPotions.Insert(0, taken);
        }

        _pocket = heldData;   // 손에 있던 게 주머니로 (없었으면 비워짐)
        _pocketUsed = true;

        canHand = true;       // 손이 비었으면 다음 포션이 내려오도록
    }

    private void Shoot(Vector2 dir)
    {
        GameObject held = currentPotions[0];
        currentPotions.RemoveAt(0);

        Potion heldPotion = held.GetComponent<Potion>();
        AbstractPotion data = heldPotion != null ? heldPotion.Data : null;

        PotionModifiers mods = BuildMods();
        int count = _augment != null ? _augment.ProjectileCount : 1;

        // 원래 던지던 속도. 이걸 좌우로 돌려서 부채꼴을 만든다.
        Vector2 baseVelocity = (dir + Vector2.up * Angle) * potionSpeed;

        for (int i = 0; i < count; i++)
        {
            // -0.5 ~ +0.5 로 벌어지게 만든다 (1개면 정중앙)
            float t = count == 1 ? 0f : (i / (float)(count - 1)) - 0.5f;
            Vector2 velocity = Quaternion.Euler(0f, 0f, t * spreadAngle) * baseVelocity;

            GameObject go = held;

            // 첫 발은 손에 든 것, 나머지는 같은 포션으로 복제한다
            if (i > 0)
            {
                if (data == null) break;

                go = SpawnPotion(data, Hand);
                if (go == null) break;
            }

            Launch(go, velocity);
        }

        _pocketUsed = false; // 던졌으니 다시 교환할 수 있다
        canHand = true;
    }

    private void Launch(GameObject go, Vector2 velocity)
    {
        go.transform.SetParent(null);
        go.GetComponent<Potion>().Release();

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.linearVelocity = velocity;
    }

    private void CreatePotion()
    {
        GameObject clone = SpawnPotion(PickPotion(), PlayerUp);
        if (clone != null) currentPotions.Add(clone);
    }

    // 포션 하나를 만들어 지정한 자리에 붙인다
    private GameObject SpawnPotion(AbstractPotion data, Transform parent)
    {
        if (data == null || data.PotionPrefab == null) return null;

        GameObject clone = Instantiate(data.PotionPrefab, parent);
        clone.GetComponent<Potion>().Init(data, BuildMods(), this);
        return clone;
    }

    // 기본 포션 + 해금으로 추가된 포션을 합쳐서 하나 뽑는다
    private AbstractPotion PickPotion()
    {
        int extra = _augment != null ? _augment.Unlocked.Count : 0;
        int total = potions.Count + extra;
        if (total <= 0) return null;

        int i = Random.Range(0, total);
        return i < potions.Count ? potions[i] : _augment.Unlocked[i - potions.Count];
    }

    private PotionModifiers BuildMods()
    {
        return _augment != null ? _augment.BuildModifiers() : PotionModifiers.None;
    }

    public IEnumerator WaitCo()
    {
        canCreate = false;

        // 여러 포션 해금을 먹으면 교체 주기가 짧아진다
        float interval = _augment != null ? _augment.CycleInterval(cycleTime) : cycleTime;
        yield return new WaitForSeconds(interval);

        canCreate = true;
    }
}
