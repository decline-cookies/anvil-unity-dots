using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    // TODO: #283 - Devise a different name to better reflect the intended use and nature of this data type
    /// <summary>
    /// An <see cref="IAbstractPersistentData"/> that is owned by the overall application and
    /// used to provide a unique instance of the data that persists for a thread index.
    /// There is only ever one <typeparamref name="T"/> per thread.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IThreadPersistentDataInstance"/></typeparam>
    public interface IThreadPersistentData<T> : IAbstractPersistentData
        where T : unmanaged, IThreadPersistentDataInstance
    {
        /// <summary>
        /// Gets a <see cref="ThreadPersistentDataAccessor{T}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="accessor">The <see cref="ThreadPersistentDataAccessor{T}"/></param>
        /// <returns>A <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireAsync(out ThreadPersistentDataAccessor<T> accessor);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="ThreadPersistentDataAccessor{T}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="ThreadPersistentDataAccessor{T}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="Release"/> when done.
        /// </summary>
        /// <returns>The <see cref="ThreadPersistentDataAccessor{TInstance}"/></returns>
        public ThreadPersistentDataAccessor<T> Acquire();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="ThreadPersistentDataAccessor{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void Release();
    }
}