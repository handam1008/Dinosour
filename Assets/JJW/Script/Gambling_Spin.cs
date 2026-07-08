
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gambling
{
    
}
public class GamblingSpin : MonoBehaviour
{
    private bool isSpin;
    private bool isJackpot;
    
    private void Update()
    {
        Spining();
    }


    private void Spining()
    {
        if (Keyboard.current.shiftKey.wasPressedThisFrame)
        {
            isSpin = true;
            Debug.Log("Spinning");
        }
        else
        {
            isSpin = false;
        }
    }
}
