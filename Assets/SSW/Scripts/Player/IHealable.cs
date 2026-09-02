namespace SSW
{
    public interface IHealable
    {
        float Current { get; }
        float Max { get; }
        void Heal(float amount);
    }
}
