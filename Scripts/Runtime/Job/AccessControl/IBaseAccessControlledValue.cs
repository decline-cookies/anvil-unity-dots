using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    public interface IBaseAccessControlledValue<T> : IAnvilDisposable
    {
        /// <summary>
        /// Gets the current <see cref="JobHandle"/> that must be completed before the provided <see cref="AccessType"/>
        /// may be performed without modifying the state of the controller.
        /// This is the same <see cref="JobHandle"/> that would be returned by
        /// <see cref="IAccessControlledValue{T}.AcquireAsync"/> or
        /// <see cref="IReadOnlyAccessControlledValue{T}.AcquireReadOnlyAsync"/> when provided the same parameter.
        /// </summary>
        /// <remarks>
        /// Generally <see cref="IAccessControlledValue{T}.AcquireAsync"/> or
        /// <see cref="IReadOnlyAccessControlledValue{T}.AcquireReadOnlyAsync"/>should be used.
        /// This method is an advanced feature for specialized
        /// situations like detecting if a value has been acquired for writing between calls.
        /// </remarks>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>
        /// A <see cref="JobHandle"/> that needs to be completed before the requested access type would be valid.
        /// </returns>
        public JobHandle GetDependencyFor(AccessType accessType);
        
        /// <summary>
        /// Releases access to the data so other callers can use it.
        /// Could potentially block on the calling thread if <see cref="IAccessControlledValue{T}.AcquireAsync"/>
        /// or <see cref="IReadOnlyAccessControlledValue{T}.AcquireReadOnlyAsync"/> was called first and the
        /// dependency returned has not yet been completed.
        ///
        /// Typically this is used when main thread work on the data is complete.
        /// </summary>
        public void Release();
        
        /// <summary>
        /// Releases access to the data so other callers can use it once the <paramref name="releaseAccessDependency"/>
        /// is complete.
        ///
        /// Typically this used when job work on the data needs to be completed before other callers can use the data
        /// again.
        /// </summary>
        /// <param name="releaseAccessDependency">The <see cref="JobHandle"/> to wait upon</param>
        public void ReleaseAsync(JobHandle releaseAccessDependency);
    }
}
