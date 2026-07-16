using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    public bool isParrying = false;
    public float parryTime = 0.2f;
    public float reflectSpeed = 20f;

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Parry();
        }
    }
    public void Parry()
    {
        if (!isParrying)
            StartCoroutine(ParryRoutine());
    }



    IEnumerator ParryRoutine()
    {
        isParrying = true;

        yield return new WaitForSeconds(parryTime);

        isParrying = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isParrying) return;

        if (!other.CompareTag("Bullet")) return;

        Debug.Log("패링 성공!");

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb == null) return;

        rb.linearVelocity = -rb.linearVelocity;
        
        rb.linearVelocity = Vector2.right * reflectSpeed; 
    }

    
}
