using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        
        if (Input.GetMouseButtonDown(0))
        {
            if(currentPotions.Count <= 0) return;
            
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
        GameObject potion = potions[currentIndex].PotionPrefab;
        potion = Instantiate(potion, PlayerUp);
        currentPotions.Enqueue(potion);
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
        yield return new WaitForSeconds(3f);
        canCreate = true;
    }
    
    
}