namespace SSW
{
    /// <summary>Health that can be restored without requiring damage capability.</summary>
    public interface IHealable
    {
        float Current { get; }
        float Max { get; }
        void Heal(float amount);
    }
}
