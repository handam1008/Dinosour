using System;
using NKY.Lib.EventChannel;
using UnityEngine;

public class JackpotElectric : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO _OnJackpot;
    [SerializeField] private VoidEventChannelSO _OnJackpotEnd;

    private void Awake()
    {
        gameObject.SetActive(false);
        _OnJackpot.OnEventRaised += OnElectric;
        _OnJackpotEnd.OnEventRaised += DelectElectric;
    }

    private void OnElectric()
    {
        gameObject.SetActive(true);
    }

    private void DelectElectric()
    {
        gameObject.SetActive(false);
    }
}
