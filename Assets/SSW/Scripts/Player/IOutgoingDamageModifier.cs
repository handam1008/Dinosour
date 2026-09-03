namespace SSW
{
    public interface IOutgoingDamageModifier
    {
        int Priority { get; }
        float ModifyOutgoingDamage(float amount);
    }
}
