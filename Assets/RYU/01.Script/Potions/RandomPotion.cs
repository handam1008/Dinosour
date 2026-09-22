using System.Collections;
using System.Collections.Generic;
using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
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
    [SerializeField] private float cycleTime = 1.5f;  
    [SerializeField] private float spreadAngle = 24f; 
    [SerializeField] private SoundClipSO throwSound;

    private WitchAugmentController _augment;

  
    private readonly List<GameObject> currentPotions = new List<GameObject>();

    
    private AbstractPotion _pocket;
    private bool _pocketUsed;
    private Camera _camera;
    
    public AbstractPotion Pocket => _pocket;

    [SerializeField] private bool canCreate = false;
    [SerializeField] private bool canHand = false;

    private void Awake()
    {
        _augment = GetComponentInParent<WitchAugmentController>();
        _camera = Camera.main;
    }

    private void Start()
    {
        canCreate = true;
        canHand = true;
        ResetPocket(); 
    }
    
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

            Vector3 mouseWorld = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = ((Vector2)mouseWorld - (Vector2)Hand.position).normalized;
            Shoot(dir);
            ServiceLocator.Get<IAudioService>().PlaySfx(throwSound);
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


    private void SwapPocket()
    {
        if (_augment == null || !_augment.Has(WitchAugmentType.Pocket)) return;
        if (_pocketUsed) return; 

        AbstractPotion heldData = null;
        
        if (currentPotions.Count > 0)
        {
            GameObject held = currentPotions[0];
            Potion potion = held.GetComponent<Potion>();
            heldData = potion != null ? potion.Data : null;

            currentPotions.RemoveAt(0);
            Destroy(held);
        }
        
        if (_pocket != null)
        {
            GameObject taken = SpawnPotion(_pocket, Hand);
            if (taken != null) currentPotions.Insert(0, taken);
        }

        _pocket = heldData;  
        _pocketUsed = true;

        canHand = true;       
    }

    private void Shoot(Vector2 dir)
    {
        GameObject held = currentPotions[0];
        currentPotions.RemoveAt(0);

        Potion heldPotion = held.GetComponent<Potion>();
        AbstractPotion data = heldPotion != null ? heldPotion.Data : null;

        PotionModifiers mods = BuildMods();
        int count = _augment != null ? _augment.ProjectileCount : 1;

        Vector2 baseVelocity = (dir + Vector2.up * Angle) * potionSpeed;

        for (int i = 0; i < count; i++)
        {
          
            float t = count == 1 ? 0f : (i / (float)(count - 1)) - 0.5f;
            Vector2 velocity = Quaternion.Euler(0f, 0f, t * spreadAngle) * baseVelocity;

            GameObject go = held;

        
            if (i > 0)
            {
                if (data == null) break;

                go = SpawnPotion(data, Hand);
                if (go == null) break;
            }

            Launch(go, velocity);
        }

        _pocketUsed = false;
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


    private GameObject SpawnPotion(AbstractPotion data, Transform parent)
    {
        if (data == null || data.PotionPrefab == null) return null;

        GameObject clone = Instantiate(data.PotionPrefab, parent);
        clone.GetComponent<Potion>().Init(data, BuildMods(), this);
        return clone;
    }

    
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

        
        float interval = _augment != null ? _augment.CycleInterval(cycleTime) : cycleTime;
        yield return new WaitForSeconds(interval);

        canCreate = true;
    }
}
