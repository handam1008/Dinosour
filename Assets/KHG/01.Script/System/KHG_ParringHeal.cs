using SSW;
using UnityEngine;

public class KHG_ParringHeal : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private MonoBehaviour healableComponent;

    private IHealable healable;
    private KHG_Paring parry;

    private void Awake()
    {
        parry = GetComponent<KHG_Paring>();

        if (healableComponent != null)
        {
            healable = healableComponent as IHealable;
        }

        if (healable == null) healable = GetComponent<IHealable>();
        if (healable == null) healable = GetComponentInParent<IHealable>();
        if (healable == null) healable = GetComponentInChildren<IHealable>();
    }

    private void OnEnable()
    {
        if (parry != null)
        {
            parry.OnParrySuccess -= HealOnParry;
            parry.OnParrySuccess += HealOnParry;
        }
    }

    private void OnDisable()
    {
        if (parry != null)
        {
            parry.OnParrySuccess -= HealOnParry;
        }
    }

    private void HealOnParry()
    {
        if (healable == null) return;

        healable.Heal(healAmount);
        Debug.Log($"★ 패링 성공! {healAmount} 회복됨 (현재 체력: {healable.Current}/{healable.Max})");
    }
}