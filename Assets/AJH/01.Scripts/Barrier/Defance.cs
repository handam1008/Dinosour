using System;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

public class Defance : MonoBehaviour, IIncomingDamageModifier
{
    [SerializeField] float _coolDown = 3f;
    [SerializeField] float _barrierContinue = 0.5f;
    [SerializeField] GameObject _barrier;

    public event Action OnBarrierUsed;

    float _guardEndTime;
    float _readyTime;

    public bool IsGuarding => Time.time < _guardEndTime;
    public bool IsReady => Time.time >= _readyTime;

    public float CoolDown
    {
        get => _coolDown;
        set => _coolDown = Mathf.Max(0f, value);
    }

    public float BarrierContinue
    {
        get => _barrierContinue;
        set => _barrierContinue = Mathf.Max(0f, value);
    }

    public int Priority => -100;

    void Awake()
    {
        if (_barrier != null) _barrier.SetActive(false);
    }

    void Update()
    {
        if (_barrier != null && _barrier.activeSelf != IsGuarding)
            _barrier.SetActive(IsGuarding);

        if (Mouse.current == null) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (!IsReady) return;

        Guard();
    }

    public void Guard()
    {
        _guardEndTime = Time.time + _barrierContinue;
        _readyTime = _guardEndTime + _coolDown;
        OnBarrierUsed?.Invoke();
    }

    public void ResetCooldown()
    {
        _readyTime = Time.time;
    }

    public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
    {
        if (!IsGuarding) return currentAmount;
        if (request.HasTag(DamageTag.IgnoreDefense)) return currentAmount;
        return 0f;
    }
}