using System;
using UnityEngine;

public class JackpotElectric : MonoBehaviour
{
    private Gamblinger _gamblinger;

    private void Awake()
    {
        _gamblinger = GetComponent<Gamblinger>();
        _gamblinger.OnJackpot += OnElectric;
    }

    private void OnElectric()
    {
        gameObject.SetActive(true);
    }
    
}
