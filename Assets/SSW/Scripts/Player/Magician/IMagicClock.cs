namespace SSW
{
    public interface IMagicClock
    {
        double Now { get; }
        bool Authority { get; }
        double ReadyAt(MagicianAugmentType type);
        void SetReady(MagicianAugmentType type, double time);
    }
}
