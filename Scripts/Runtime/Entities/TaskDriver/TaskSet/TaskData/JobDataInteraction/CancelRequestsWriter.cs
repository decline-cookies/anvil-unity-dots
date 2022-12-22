using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Job-Safe struct to allow for requesting the cancellation by <see cref="Entity"/>
    /// </summary>
    [BurstCompatible]
    public struct CancelRequestsWriter
    {
        // private DataStreamPendingWriter<CancelRequest> m_PendingWriter;
        // internal CancelRequestsWriter(DataStreamPendingWriter<CancelRequest> pendingWriter) : this()
        // {
        //     m_PendingWriter = pendingWriter;
        // }

        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <remarks>
        /// In most cases this will be called automatically by the Anvil Job type. If using this in a vanilla Unity
        /// Job type, this must be called manually before any other interaction with this struct.
        /// </remarks>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        public void InitForThread(int nativeThreadIndex)
        {
            // m_PendingWriter.InitForThread(nativeThreadIndex);
        }

        /// <summary>
        /// Requests the cancellation of a TaskDriver flow for a specific <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to use for cancellation</param>
        public void RequestCancel(Entity entity)
        {
            RequestCancel(ref entity);
        }

        /// <inheritdoc cref="RequestCancel(Entity)"/>
        public void RequestCancel(ref Entity entity)
        {
            // m_PendingWriter.Add(new CancelRequest(entity));
        }
    }
}
