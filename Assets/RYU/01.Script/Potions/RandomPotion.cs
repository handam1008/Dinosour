using System.Collections;
using System.Collections.Generic;
using RYU._01.Script.Potions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class RandomPotion : MonoBehaviour
{
    [SerializeField] private List<AbstractPotion> potions = new List<AbstractPotion>();
    [SerializeField] private Transform Hand;
    [SerializeField] private Transform PlayerUp;
    [SerializeField] private float potionSpeed = 15f;
    [SerializeField] private float Angle = 15f;
    private int currentIndex = 0;


    private Queue<GameObject> currentPotions = new Queue<GameObject>();

    [SerializeField] private bool canCreate = false;
    [SerializeField] private bool canHand = false;

    private void Start()
    {
        canCreate = true;
        canHand = true;
    }

    private void Update()
    {
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(currentPotions.Count <= 0) return;
            
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
            potion.transform.SetParent(Hand,false);
            canHand = false;
        }
        
    }

    private void Shoot(Vector2 dir)
    {
        GameObject potion = currentPotions.Dequeue();
        potion.transform.SetParent(null); 
        potion.GetComponent<Potion>().Release();
        Rigidbody2D rb = potion.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3;
        rb.linearVelocity = (dir + Vector2.up * Angle) * potionSpeed;
        canHand = true;
        Debug.Log("슛");
    }

    private void CreatePotion()
    {
        RandomValue();
        AbstractPotion data = potions[currentIndex];
        GameObject clone = Instantiate(data.PotionPrefab, PlayerUp);
        clone.GetComponent<Potion>().Init(data, 1f ,this); 
        currentPotions.Enqueue(clone);
        Debug.Log($"큐 개수: {currentPotions.Count}");

    }

    public int RandomValue()
    {
        currentIndex = Random.Range(0, potions.Count);
        return currentIndex;
    }

    public IEnumerator WaitCo()
    {
        canCreate = false;
        yield return new WaitForSeconds(1.5f);
        canCreate = true;
    }
    
    
}