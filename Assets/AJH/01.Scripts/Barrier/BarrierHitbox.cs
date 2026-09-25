using UnityEngine;

namespace SSW
{
    public class BarrierHitbox : MonoBehaviour, IDamageable, IDamageReceiver
    {
        Health _owner;

        public float Current => _owner.Current;
        public float Max => _owner.Max;

        public void Bind(Health owner) => _owner = owner;

        public DamageResult ReceiveDamage(DamageRequest request) => _owner.ReceiveDamage(request);
        public void TakeDamage(float amount) => _owner.TakeDamage(amount);
        public void TakeDamage(float amount, bool isCritical) => _owner.TakeDamage(amount, isCritical);
        public void Heal(float amount) => _owner.Heal(amount);
    }
}