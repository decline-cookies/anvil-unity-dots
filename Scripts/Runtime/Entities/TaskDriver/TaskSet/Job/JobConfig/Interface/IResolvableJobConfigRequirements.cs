namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// This job type can resolve from it's current data into different types of "result" data.
    /// </summary>
    public interface IResolvableJobConfigRequirements : IJobConfig
    {
        /// <summary>
        /// A delegate that configures the requirements for a job on the provided <see cref="IResolvableJobConfigRequirements"/>.
        /// The same <see cref="IResolvableJobConfigRequirements"/> instance should be returned by the method to allow
        /// for chaining of additional requirements.
        /// </summary>
        /// <param name="taskDriver">
        /// The task driver instance that is configuring the job. This may be used to gain access to streams or other
        /// task driver specific references.
        /// </param>
        /// <param name="jobConfig">The job config instance to set requirements on.</param>
        /// <typeparam name="T">The concrete type of the <see cref="AbstractTaskDriver"/></typeparam>
        /// <returns>
        /// A reference to the <see cref="IResolvableJobConfigRequirements"/> instance passed in to continue chaining
        /// configuration methods.
        /// </returns>
        public delegate IResolvableJobConfigRequirements ConfigureJobRequirementsDelegate<in T>(T taskDriver, IResolvableJobConfigRequirements jobConfig)
            where T : AbstractTaskDriver;

        /// <summary>
        /// Requires a specific type of data that the job can resolve itself into.
        /// </summary>
        /// <typeparam name="TResolveTargetType">The type of data to be able to resolve to.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance;

        /// <summary>
        /// Specifies a delegate to call to add additional requirements.
        /// This allows requirements to be defined by a static method on the job rather than at the configuration call
        /// site.
        /// </summary>
        /// <param name="taskDriver">The task driver instance that the job is being configured on. (usually this)</param>
        /// <param name="configureRequirements">The delegate to call to configure requirements.</param>
        /// <typeparam name="T">The type of the task driver instance.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        IResolvableJobConfigRequirements AddRequirementsFrom<T>(T taskDriver, ConfigureJobRequirementsDelegate<T> configureRequirements)
            where T : AbstractTaskDriver;
    }
}