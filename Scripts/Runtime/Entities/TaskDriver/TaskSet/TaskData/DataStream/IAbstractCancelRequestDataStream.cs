using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractDataStream"/> that represents a cancel request for an <see cref="Entity"/>
    /// </summary>
    public interface IAbstractCancelRequestDataStream : IAbstractDataStream
    {
        /// <summary>
        /// Gets a <see cref="CancelRequestsWriter"/> for use in a job.
        /// Requires a call to <see cref="ReleaseCancelRequestsWriterAsync"/> after scheduling the job.
        /// </summary>
        /// <param name="cancelRequestsWriter">The <see cref="CancelRequestsWriter"/></param>
        /// <returns>A <see cref="JobHandle"/> to wait on</returns>
        public JobHandle AcquireCancelRequestsWriterAsync(out CancelRequestsWriter cancelRequestsWriter);

        /// <summary>
        /// Allows other jobs to use the underlying data for the <see cref="CancelRequestsWriter"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> that used this data.</param>
        public void ReleaseCancelRequestsWriterAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets a <see cref="CancelRequestsWriter"/> for use on the main thread.
        /// Requires a call to <see cref="ReleaseCancelRequestsWriter"/> when done.
        /// </summary>
        /// <returns>The <see cref="CancelRequestsWriter"/></returns>
        public CancelRequestsWriter AcquireCancelRequestsWriter();

        /// <summary>
        /// Allows other jobs or code to use to underlying data for the <see cref="CancelRequestsWriter"/>
        /// and ensures data integrity across those other usages.
        /// </summary>
        public void ReleaseCancelRequestsWriter();
    }
}
