namespace SSW
{
    /// <summary>
    /// Optional combat capability. Attacks query this interface instead of a concrete player class.
    /// </summary>
    public interface IOutgoingDamageModifier
    {
        float ModifyOutgoingDamage(float amount);
    }
}
