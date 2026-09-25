namespace SSW
{
    public interface ILiftZone
    {
        bool Active { get; }
        float RiseSpeed { get; }
        float Acceleration { get; }
    }
}
