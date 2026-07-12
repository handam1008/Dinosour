using System;
using Unity.VisualScripting;
using UnityEngine;

public class KHG_Paring : MonoBehaviour
{
    private float parrying = 0.5f;
    private bool isParrying = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && !isParrying)
        {
            
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        
    }
}
