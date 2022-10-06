using System;

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
        /// <see cref="TaskStream{TInstance}"/>s on the governing <see cref="AbstractTaskDriver"/> and
        /// <see cref="AbstractTaskSystem"/> will be required for writing in a shared-write context.
        /// </summary>
        /// <param name="resolveTarget">The identifier for the target <see cref="TaskStream{TInstance}"/>s</param>
        /// <typeparam name="TResolveTarget">The type of identifier</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum;
    }
}
