using System.Collections.Generic;
using UnityEngine;

public class RandomPotion : MonoBehaviour
{
    [SerializeField] private List<AbstractPotion> potions = new List<AbstractPotion>();
    [SerializeField] private Transform Hand;
    [SerializeField] private Transform PlayerUp;
    [SerializeField] private float cycleTime = 3f;
    [SerializeField] private float throwPower = 10f;

    private GameObject headPotion;
    private GameObject handPotion;
    private float timer;

    private void Start()
    {
        headPotion = SpawnAtHead();
        Advance();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Throw();
            timer = 0f;
        }

        timer += Time.deltaTime;
        if (timer >= cycleTime)
        {
            timer = 0f;
            AutoCycle();
        }
    }

    private void AutoCycle()
    {
        if (handPotion != null) Destroy(handPotion);
        Advance();
    }

    private void Throw()
    {
        if (handPotion == null) return;

        handPotion.transform.SetParent(null);

        Rigidbody2D rb = handPotion.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = ((Vector2)(mouseWorld - Hand.position)).normalized;
            rb.linearVelocity = dir * throwPower;
        }

        handPotion = null;
        Advance();
    }

    private void Advance()
    {
        handPotion = headPotion;
        handPotion.transform.SetParent(Hand, false);
        handPotion.transform.localPosition = Vector3.zero;

        headPotion = SpawnAtHead();
    }

    private GameObject SpawnAtHead()
    {
        int i = Random.Range(0, potions.Count);
        GameObject clone = Instantiate(potions[i].PotionPrefab, PlayerUp);
        clone.transform.localPosition = Vector3.zero;
        return clone;
    }
}