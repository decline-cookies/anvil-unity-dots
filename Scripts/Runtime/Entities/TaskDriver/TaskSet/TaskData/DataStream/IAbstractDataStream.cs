using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractDataStream"/> typed to a specific <see cref="IEntityKeyedTask"/>
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/></typeparam>
    public interface IAbstractDataStream<TInstance> : IAbstractDataStream
        where TInstance : unmanaged, IEntityKeyedTask
    {
        /// <summary>
        /// Gets the configured <see cref="CancelRequestBehaviour"/> for the data stream.
        /// </summary>
        public CancelRequestBehaviour CancelBehaviour { get; }

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
        /// <summary>
        /// The version of the data. This value is incremented each time write access is granted to the data allowing
        /// consumers to check whether the data may have changed since the last time the consumer read it.
        /// </summary>
        /// <remarks>
        /// Store this value at the time of read access and on next potential read use <see cref="IsActiveDataInvalidated"/>
        /// to check whether the data has potentially changed. This is the safest option as it accounts for other potential
        /// conditions in the data.
        ///
        /// If comparing version numbers directly make sure to always use an equality check. The version number can
        /// theoretically wrap and overflow so it's possible for the current value to be less than the previous value.
        /// </remarks>
        public uint ActiveDataVersion { get; }
        
        /// <summary>
        /// Whether the underlying data has potentially been updated by something getting write access to it.
        /// </summary>
        /// <param name="lastVersion">
        /// A write version at the last read.
        /// </param>
        /// <returns>true if the data has potentially been updated, false if not</returns>
        public bool IsActiveDataInvalidated(uint lastVersion);
    }
}