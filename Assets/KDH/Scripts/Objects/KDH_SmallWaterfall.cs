using System;
using UnityEngine;

public class KDH_SmallWaterfall : MonoBehaviour
{
    [SerializeField] private float addForceAmount;
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Rigidbody2D  rigidBody))
            rigidBody.AddForceY(addForceAmount, ForceMode2D.Impulse);
    }
}
