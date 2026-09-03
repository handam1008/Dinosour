using System;
using UnityEngine;

public class Gamblinger : MonoBehaviour
{
    //[SerializeField] private 
    public bool Isjackpot { get; private set; }
    public event Action OnJackpot;
    public event Action OnJackpotEnd;
    
    public void JackPot() => OnJackpot?.Invoke();
    public void JackPotEnd() => OnJackpotEnd?.Invoke();
}
