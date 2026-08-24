using SSW;
using UnityEngine;

public class KHG_ParringHeal : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;

    private Health health;
    private KHG_Paring parry;

    private void Awake()
    {
        health = GetComponent<Health>();
        parry = GetComponent<KHG_Paring>();
    }

    private void OnEnable()
    {
        if (parry != null)
            parry.OnParrySuccess += HealOnParry;
    }

    private void OnDisable()
    {
        if (parry != null)
            parry.OnParrySuccess -= HealOnParry;
    }

    private void HealOnParry()
    {
        if (health == null) return;

        health.Heal(healAmount);

        Debug.Log("힐중");

    }
}
