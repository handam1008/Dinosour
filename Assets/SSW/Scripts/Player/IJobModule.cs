namespace SSW
{
    public interface IJobModule
    {
        PlayerJob Job { get; }
        bool IsJobActive { get; }
        void SetJobActive(bool active);
    }
}
