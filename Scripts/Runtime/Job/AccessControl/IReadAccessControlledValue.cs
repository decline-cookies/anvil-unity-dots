using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// An interface that describes an access controlled value that grants read access.
    /// </summary>
    /// <typeparam name="T">The type of value being access controlled.</typeparam>
    public interface IReadAccessControlledValue<T> : IAnvilDisposable
    {
        /// <summary>
        /// Acquires the <see cref="T"/> instance synchronously for Read Only access and returns the controlled instance.
        ///
        /// This is the preferred method of synchronous value access vs
        /// <see cref="AcquireRead"/>/<see cref="IReadAccessControlledValue{T}.Release"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="AccessControlledValue{T}.AccessHandle"/> is a safer way to synchronously maintain access to
        /// an access controlled value. Paired with a using statement access to the value will be released when the
        /// handle falls out of scope.
        /// </remarks>
        /// <example>using var valueHandle = myAccessControlledValue.AcquireWithReadHandle();</example>
        /// <returns>
        /// The <see cref="AccessControlledValue{T}.AccessHandle"/> that maintains access to the controlled value until
        /// disposed.
        /// </returns>
        public AccessControlledValue<T>.AccessHandle AcquireWithReadHandle();

        /// <summary>
        /// Acquires the <see cref="T"/> instance synchronously for Read Only access.
        /// Will block on the calling thread if there are any jobs that need to complete before this data instance
        /// can be used.
        ///
        /// Typically this is used when wanting to perform main thread work on the data.
        /// </summary>
        /// <returns>The data instance</returns>
        public T AcquireRead();

        /// <summary>
        /// Acquires the <see cref="T"/> instance asynchronously for Read Only access.
        /// The data will be returned immediately as well as a <see cref="JobHandle"/> to schedule actually
        /// reading from the data.
        ///
        /// Not respecting the <see cref="JobHandle"/> could lead to dependency errors.
        ///
        /// Typically this is used when wanting to perform work on the data in a job to be scheduled.
        /// </summary>
        /// <param name="value">The data instance</param>
        /// <returns>A <see cref="JobHandle"/> to wait on before accessing the data</returns>
        public JobHandle AcquireReadAsync(out T value);

        /// <summary>
        /// Releases access to the data so other callers can use it.
        /// Could potentially block on the calling thread if access was acquired asynchronously and the dependency
        /// returned has not yet been completed.
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