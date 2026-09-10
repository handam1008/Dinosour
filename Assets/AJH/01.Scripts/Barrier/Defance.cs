using System;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

public class Defance : MonoBehaviour, IIncomingDamageModifier
{
    [SerializeField] float _coolDown = 3f;            // 방어 쿨타임
    [SerializeField] float _barrierContinue = 0.5f;   // 방어 유지시간
    [SerializeField] GameObject _barrier;             // 보이는 이펙트 (없어도 동작함)

    public event Action OnBarrierUsed;

    float _guardEndTime;
    float _readyTime;

    public bool IsGuarding => Time.time < _guardEndTime;
    public bool IsReady => Time.time >= _readyTime;

    // 방어 증강(쿨감, 유지시간 증가)이 조절할 값
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

    // 다른 받는 피해 보정들보다 먼저 계산
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

    void Guard()
    {
        _guardEndTime = Time.time + _barrierContinue;
        _readyTime = _guardEndTime + _coolDown;   // 유지시간이 끝난 뒤부터 쿨타임
        OnBarrierUsed?.Invoke();
    }

    public float ModifyIncomingDamage(DamageRequest request, float currentAmount)
    {
        if (!IsGuarding) return currentAmount;
        if (request.HasTag(DamageTag.IgnoreDefense)) return currentAmount;
        return 0f;
    }
}