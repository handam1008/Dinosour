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

    private Queue<GameObject> currentPotions = new Queue<GameObject>();

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
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentPotions.Count <= 0) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = ((Vector2)mouseWorld - (Vector2)Hand.position).normalized;
            Shoot(dir);
        }

        if (currentPotions.Count > 2)
        {
            GameObject potion = currentPotions.Dequeue();
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

            GameObject potion = currentPotions.Peek();
            potion.transform.SetParent(Hand, false);
            canHand = false;
        }
    }

    private void Shoot(Vector2 dir)
    {
        GameObject held = currentPotions.Dequeue();
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
                if (data == null || data.PotionPrefab == null) break;

                go = Instantiate(data.PotionPrefab, Hand);
                go.transform.localPosition = Vector3.zero;
                go.GetComponent<Potion>().Init(data, mods, this);
            }

            Launch(go, velocity);
        }

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
        AbstractPotion data = PickPotion();
        if (data == null || data.PotionPrefab == null) return;

        GameObject clone = Instantiate(data.PotionPrefab, PlayerUp);
        clone.GetComponent<Potion>().Init(data, BuildMods(), this);
        currentPotions.Enqueue(clone);
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
