using UnityEngine;

public class KHG_DashCooldown : MonoBehaviour
{
    [Header("연결할 컴포넌트")]
    [SerializeField] private KHG_Paring paringComponent;
    [SerializeField] private KHG_Dash dashComponent;

    [Header("설정")]
    [SerializeField] private float cooldownReduction = 3.0f; // 쿨타임 3초 감소


    private void Awake()
    {
        if (paringComponent == null) paringComponent = GetComponent<KHG_Paring>();
        if (dashComponent == null) dashComponent = GetComponent<KHG_Dash>();
    }

    private void OnEnable()
    {
        if (paringComponent != null)
        {
            paringComponent.OnParrySuccess += ApplyCooldownReduction;
        }
    }

    private void OnDisable()
    {
        if (paringComponent != null)
        {
            paringComponent.OnParrySuccess -= ApplyCooldownReduction;
        }
    }

    private void ApplyCooldownReduction()
    {
        if (dashComponent != null)
        {
            dashComponent.ReduceCooldown(cooldownReduction);
        }
    }
}
