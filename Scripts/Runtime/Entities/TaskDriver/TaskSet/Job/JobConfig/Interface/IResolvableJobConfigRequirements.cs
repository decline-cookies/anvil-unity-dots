namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// This job type can resolve from it's current data into different types of "result" data.
    /// </summary>
    public interface IResolvableJobConfigRequirements : IJobConfig
    {
        /// <summary>
        /// Requires a specific type of data that the job can resolve itself into.
        /// </summary>
        /// <typeparam name="TResolveTargetType">The type of data to be able to resolve to.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance;
    }
}
