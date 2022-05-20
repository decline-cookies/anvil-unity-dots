using Anvil.CSharp.Core;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A class that wraps a data object of type <typeparamref name="T"/> and a <see cref="AccessController"/>
    /// to allow for safe access to the data object.
    /// </summary>
    /// <typeparam name="T">The type of data to wrap access control to</typeparam>
    public class AccessControlledValue<T> : AbstractAnvilBase
    {
        protected T Value
        {
            get;
            set;
        }

        private readonly AccessController m_AccessController;

        /// <summary>
        /// Creates a new instance of <see cref="AccessControlledValue{T}"/> for the passed in
        /// data.
        /// </summary>
        /// <param name="value">The data instance to wrap access control around.</param>
        public AccessControlledValue(T value)
        {
            Value = value;
            m_AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            m_AccessController.Dispose();
            (Value as IDisposable)?.Dispose();
            base.DisposeSelf();
        }

        /// <summary>
        /// Acquires the data instance synchronously for a given <see cref="AccessType"/>
        /// Will block on the calling thread if there are any jobs that need to complete before this data instance
        /// can be used.
        ///
        /// Typically this is used when wanting to perform main thread work on the data.
        /// </summary>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>The data instance</returns>
        public T Acquire(AccessType accessType)
        {
            m_AccessController.Acquire(accessType);
            return Value;
        }

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
        public JobHandle AcquireAsync(AccessType accessType, out T value)
        {
            value = Value;
            return m_AccessController.AcquireAsync(accessType);
        }

        /// <summary>
        /// Releases access to the data so other callers can use it.
        /// Could potentially block on the calling thread if <see cref="AcquireAsync"/> was called first and the
        /// dependency returned has not yet been completed.
        ///
        /// Typically this is used when main thread work on the data is complete.
        /// </summary>
        public void Release()
        {
            m_AccessController.Release();
        }

        /// <summary>
        /// Releases access to the data so other callers can use it once the <paramref name="releaseAccessDependency"/>
        /// is complete.
        ///
        /// Typically this used when job work on the data needs to be completed before other callers can use the data
        /// again.
        /// </summary>
        /// <param name="releaseAccessDependency">The <see cref="JobHandle"/> to wait upon</param>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
