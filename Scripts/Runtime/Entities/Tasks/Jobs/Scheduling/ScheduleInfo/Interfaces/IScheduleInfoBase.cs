namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Common interface for job scheduling parameters.
    /// </summary>
    public interface IScheduleInfoBase
    {
        /// <summary>
        /// The number of elements in a batch
        /// </summary>
        public int BatchSize { get; }
    }
}
