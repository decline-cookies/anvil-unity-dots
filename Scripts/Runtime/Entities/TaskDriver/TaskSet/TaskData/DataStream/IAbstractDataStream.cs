using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractDataStream"/> typed to a specific <see cref="IEntityProxyInstance"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/></typeparam>
    public interface IAbstractDataStream<TInstance> : IAbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        /// <summary>
        /// Gets a <see cref="DataStreamActiveReader{TInstance}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleaseActiveReaderAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="reader">The <see cref="DataStreamActiveReader{TInstance}"/></param>
        /// <returns>A <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireActiveReaderAsync(out DataStreamActiveReader<TInstance> reader);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="DataStreamActiveReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseActiveReaderAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="DataStreamActiveReader{TInstance}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleaseActiveReader"/> when done.
        /// </summary>
        /// <returns>The <see cref="DataStreamActiveReader{TInstance}"/></returns>
        public DataStreamActiveReader<TInstance> AcquireActiveReader();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="DataStreamActiveReader{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseActiveReader();

        /// <summary>
        /// Gets a <see cref="DataStreamPendingWriter{TInstance}"/> for use in a job outside the Task Driver context.
        /// Requires a call to <see cref="ReleasePendingWriterAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="writer">The <see cref="DataStreamPendingWriter{TInstance}"/></param>
        /// <returns>The <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquirePendingWriterAsync(out DataStreamPendingWriter<TInstance> writer);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="DataStreamPendingWriter{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleasePendingWriterAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="DataStreamPendingWriter{TInstance}"/> for use on the main thread outside the Task Driver
        /// context.
        /// Requires a call to <see cref="ReleasePendingWriter"/> when done.
        /// </summary>
        /// <returns>The <see cref="DataStreamPendingWriter{TInstance}"/></returns>
        public DataStreamPendingWriter<TInstance> AcquirePendingWriter();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="DataStreamPendingWriter{TInstance}"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleasePendingWriter();
    }


    /// <summary>
    /// Represents a stream of data with two parts.
    /// The first is a parallel writable collection to be able to write pending instances to.
    /// The second is a narrow array collection for reading.
    /// </summary>
    public interface IAbstractDataStream
    {
        
    }
}
