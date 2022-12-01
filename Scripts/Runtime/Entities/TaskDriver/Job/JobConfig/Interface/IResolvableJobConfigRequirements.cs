namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// Extending on <see cref="IJobConfigRequirements"/> it allows for requiring resolve targets.
    /// </summary>
    public interface IResolvableJobConfigRequirements : IJobConfigRequirements
    {
        /// <summary>
        /// Specifies a target to allow instances of data to resolve to. All matching
        /// <see cref="IAbstractDataStream{TInstance}"/>s on the governing <see cref="AbstractTaskDriver"/> and
        /// <see cref="AbstractTaskDriverSystem"/> will be required for writing in a shared-write context.
        /// </summary>
        /// <typeparam name="TResolveTargetType">The type of <see cref="IEntityProxyInstance"/> that will be resolved.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance;
    }
}
