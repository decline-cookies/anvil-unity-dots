namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// </summary>
    public interface IJobConfig
    {
        /// <summary>
        /// Whether the Job is enabled or not.
        /// A job that is not enabled will not be scheduled or run but will still exist as part of the
        /// <see cref="ITaskSystem"/> or <see cref="ITaskDriver"/> that it is a part of.
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Configuration helper that will run this job only once.
        /// After being run, it will set <see cref="IsEnabled"/> to false.
        /// </summary>
        /// <remarks>
        /// This is useful for initial setup jobs.
        /// </remarks>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RunOnce();
    }
}
