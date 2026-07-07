using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    public event System.Action OnDamaged;

    float current;

    public float Current => current;
    public float Max => maxHealth;

    void Awake()
    {
        current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        current = Mathf.Max(current - amount, 0f);
        OnDamaged?.Invoke();
        if (current <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        current = Mathf.Min(current + amount, maxHealth);
    }

    void Die()
    {
        gameObject.SetActive(false);
    }
}
