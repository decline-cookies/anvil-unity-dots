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
        /// <see cref="AbstractTaskDriverSystem"/> or <see cref="AbstractTaskDriver"/> that it is associated with.
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// A configuration helper that will run this job only once.
        /// After being run, it will set <see cref="IsEnabled"/> to false.
        /// </summary>
        /// <remarks>
        /// This is useful for the initial setup jobs.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RunOnce();
    }
}
