namespace SSW
{
    /// <summary>
    /// A self-contained feature that belongs to one job.
    /// Implement this contract to add a skill without referencing another job's code.
    /// </summary>
    public interface IJobModule
    {
        PlayerJob Job { get; }
        bool IsJobActive { get; }
        void SetJobActive(bool active);
    }
}
