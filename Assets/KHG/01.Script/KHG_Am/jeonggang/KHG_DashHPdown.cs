using RYU._01.Script.Potions;
using UnityEngine;

public class KHG_DashHPdown : MonoBehaviour
{
    [Header("지속 데미지(DOT) 설정")]
    [SerializeField] private float _dotDamage = 3f;
    [SerializeField] private int _tickCount = 2;
    [SerializeField] private float _tickInterval = 0.7f;

    private KHG_Dash _dashScript;

    private void Awake()
    {
        _dashScript = GetComponent<KHG_Dash>();
    }

    private void OnEnable()
    {
        if (_dashScript != null)
            _dashScript.OnDashHitEnemy += ApplyDotDamage;
    }

    private void OnDisable()
    {
        // 스크립트가 꺼지면 연결 해제
        if (_dashScript != null)
            _dashScript.OnDashHitEnemy -= ApplyDotDamage;
    }

    private void ApplyDotDamage(GameObject target)
    {
        if (!target.TryGetComponent(out OverTimeRunner runner))
        {
            runner = target.AddComponent<OverTimeRunner>();
        }

        Component sourceComponent = transform;
        runner.Run(new DamageTick(_dotDamage, sourceComponent), _tickCount, _tickInterval);
    }
}