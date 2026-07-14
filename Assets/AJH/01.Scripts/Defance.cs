using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Defance : MonoBehaviour
{
    [SerializeField] private float _coolDown;
    [SerializeField] private GameObject _berar;

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {

        }
    }
}
