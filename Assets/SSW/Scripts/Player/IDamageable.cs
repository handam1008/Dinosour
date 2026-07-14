namespace SSW
{
    public interface IDamageable
    {
        float Current { get; }
        float Max { get; }
        void TakeDamage(float amount);
        void TakeDamage(float amount, bool isCritical);
        void Heal(float amount);
    }
}