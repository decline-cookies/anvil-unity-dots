using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    public interface IAccessControlledValue<T> : IReadOnlyAccessControlledValue<T>
    {
        /// <summary>
        /// Acquires the data instance synchronously for a given <see cref="AccessType"/> and returns the data in an
        /// <see cref="AccessControlledValue{T}.AccessHandle"/>.
        /// This is the preferred method of synchronous value access vs <see cref="Acquire"/>/<see cref="IBaseAccessControlledValue{T}.Release"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="AccessControlledValue{T}.AccessHandle"/> is a safer way to synchronously maintain access to an
        /// <see cref="AccessControlledValue{T}"/>. Paired with a using statement access to the value will be released
        /// when the handle falls out of scope.
        /// </remarks>
        /// <example>using var valueHandle = myAccessControlledValue.AcquireWithHandle(AccessType.SharedRead);</example>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>
        /// The <see cref="AccessControlledValue{T}.AccessHandle"/> that maintains access to the controlled value until disposed.
        /// </returns>
        public AccessControlledValue<T>.AccessHandle AcquireWithHandle(AccessType accessType);
        
        /// <summary>
        /// Acquires the data instance synchronously for a given <see cref="AccessType"/>.
        /// Will block on the calling thread if there are any jobs that need to complete before this data instance
        /// can be used.
        ///
        /// Typically this is used when wanting to perform main thread work on the data.
        /// </summary>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>The data instance</returns>
        public T Acquire(AccessType accessType);
        
        /// <summary>
        /// Acquires the data instance asynchronously for a given <see cref="AccessType"/>
        /// The data will be returned immediately as well as a <see cref="JobHandle"/> to schedule actually
        /// reading from/writing to the data.
        ///
        /// Not respecting the <see cref="JobHandle"/> could lead to dependency errors.
        ///
        /// Typically this is used when wanting to perform work on the data in a job to be scheduled.
        /// </summary>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <param name="value">The data instance</param>
        /// <returns>A <see cref="JobHandle"/> to wait on before accessing the data</returns>
        public JobHandle AcquireAsync(AccessType accessType, out T value);
    }
}
