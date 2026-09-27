using System;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

public class Defance : MonoBehaviour, IIncomingDamageModifier, ICooldownSource
{
    [SerializeField] float _coolDown = 3f;
    [SerializeField] float _barrierContinue = 0.5f;
    Health _health;
    [SerializeField] BarrierHitbox _barrier;

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
        _barrier.Bind(GetComponent<Health>());
        _barrier.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_barrier.gameObject.activeSelf != IsGuarding)
            _barrier.gameObject.SetActive(IsGuarding);

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

    public bool TryGetCooldown(out float remaining, out float duration)
    {
        remaining = _readyTime - Time.time;
        duration = _readyTime - _guardEndTime;
        return !IsGuarding && !IsReady && duration > 0f;
    }

    public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
    {
        if (!IsGuarding) return currentAmount;
        if (request.HasTag(DamageTag.IgnoreDefense)) return currentAmount;
        return 0f;
    }
}