using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Defance : MonoBehaviour
{
    public float _coolDown;
    private bool _checkCoolTime;
    public float _barrierContinue = 0.5f;
    [SerializeField] private GameObject _barrier;

    public event Action OnBarrierUsed;

    private void Awake()
    {
        _barrier.SetActive(false);
        _checkCoolTime = true;
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && _checkCoolTime)
        {
            OnBarrierUsed?.Invoke();

            StartCoroutine(BarrierCoolTime());
        }
    }

    IEnumerator BarrierCoolTime()
    {
        _barrier.SetActive(true);
        _checkCoolTime = false;
        
        yield return new WaitForSeconds(_barrierContinue);
        _barrier.SetActive(false);
        
        StartCoroutine(CoolTime());
    }

    IEnumerator CoolTime()
    {
        yield return new WaitForSeconds(_coolDown);
        _checkCoolTime = true;
    }
}