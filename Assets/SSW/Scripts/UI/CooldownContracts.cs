namespace SSW
{
    /// <summary>
    /// Supplies cooldown time to any UI without depending on a concrete skill or job.
    /// A skill can implement this interface and bind itself directly to RadialCooldownUI.
    /// </summary>
    public interface ICooldownSource
    {
        bool TryGetCooldown(out float remaining, out float duration);
    }
}
