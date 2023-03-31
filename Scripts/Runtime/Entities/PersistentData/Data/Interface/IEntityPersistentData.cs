using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="IAbstractPersistentData"/> typed to a specific <see cref="IEntityPersistentDataInstance"/>
    /// The data is associated with an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface IReadOnlyEntityPersistentData<T> : IAbstractPersistentData
        where T : struct, IEntityPersistentDataInstance
    {
        /// <summary>
        /// Gets a <see cref="EntityPersistentDataReader{TInstance}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseReaderAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="reader">The <see cref="EntityPersistentDataReader{TInstance}"/></param>
        /// <returns>A <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireReaderAsync(out EntityPersistentDataReader<T> reader);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="EntityPersistentDataReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseReaderAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="EntityPersistentDataReader{TInstance}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseReader"/> when done.
        /// </summary>
        /// <returns>The <see cref="EntityPersistentDataReader{TInstance}"/></returns>
        public EntityPersistentDataReader<T> AcquireReader();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="EntityPersistentDataReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseReader();
    }

    /// <summary>
    /// An <see cref="IAbstractPersistentData"/> typed to a specific <see cref="IEntityPersistentDataInstance"/>
    /// The data is associated with an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
    public interface IEntityPersistentData<T> : IAbstractPersistentData, IReadOnlyEntityPersistentData<T>
        where T : struct, IEntityPersistentDataInstance
    {
        /// <summary>
        /// Gets a <see cref="EntityPersistentDataReader{TInstance}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseReaderAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="reader">The <see cref="EntityPersistentDataReader{TInstance}"/></param>
        /// <returns>A <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireReaderAsync(out EntityPersistentDataReader<T> reader);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="EntityPersistentDataReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseReaderAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="EntityPersistentDataReader{TInstance}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseReader"/> when done.
        /// </summary>
        /// <returns>The <see cref="EntityPersistentDataReader{TInstance}"/></returns>
        public EntityPersistentDataReader<T> AcquireReader();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="EntityPersistentDataReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseReader();

        /// <summary>
        /// Gets a <see cref="EntityPersistentDataWriter{TInstance}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseWriterAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="writer">The <see cref="EntityPersistentDataWriter{TInstance}"/></param>
        /// <returns>The <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireWriterAsync(out EntityPersistentDataWriter<T> writer);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="EntityPersistentDataWriter{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseWriterAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="EntityPersistentDataWriter{TInstance}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseWriter"/> when done.
        /// </summary>
        /// <returns>The <see cref="EntityPersistentDataWriter{TInstance}"/></returns>
        public EntityPersistentDataWriter<T> AcquireWriter();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="EntityPersistentDataWriter{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseWriter();
    }
}