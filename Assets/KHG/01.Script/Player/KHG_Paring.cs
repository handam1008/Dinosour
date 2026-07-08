using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Paring : MonoBehaviour
{
    private Rigidbody2D _rb;

    private void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            StartCoroutine(OnParing());
        }
    }

    private IEnumerator OnParing()
    {
        yield return new WaitForSeconds(1f);
    }
}
