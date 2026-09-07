using System;
using NKY.Lib.EventChannel;
using UnityEngine;

public class Gamblinger : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO _OnJackpot;
    [SerializeField] private VoidEventChannelSO _OnJackpotEnd;
    public bool Isjackpot { get; private set; }

    public void JackPot() => _OnJackpot?.Raise();
    public void JackPotEnd() => _OnJackpotEnd?.Raise();
}
