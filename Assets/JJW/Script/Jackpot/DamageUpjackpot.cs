using SSW;
using UnityEngine;

public class DamageUpjackpot : MonoBehaviour, IDamageable
{
    public float Current { get; }
    public float Max { get; }
    public void TakeDamage(float amount)
    {
        throw new System.NotImplementedException();
    }

    public void TakeDamage(float amount, bool isCritical)
    {
        throw new System.NotImplementedException();
    }

    public void Heal(float amount)
    {
        throw new System.NotImplementedException();
    }
}
