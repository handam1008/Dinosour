namespace SSW
{
    public interface ICooldownSource
    {
        bool TryGetCooldown(out float remaining, out float duration);
    }
}
