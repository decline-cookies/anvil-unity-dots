using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a <see cref="AbstractTaskDriverSystem"/> through the context of the calling
    /// <see cref="AbstractTaskDriver"/>.
    /// </summary>
    public interface ITaskDriverSystem
    {
        /// <summary>
        /// Data Stream representing requests to Cancel an <see cref="Entity"/> on the system.
        /// </summary>
        public ISystemCancelRequestDataStream CancelRequestDataStream { get; }

        /// <summary>
        /// Data Stream representing when Cancel Requests are Complete on the system.
        /// </summary>
        public ISystemDataStream<CancelComplete> CancelCompleteDataStream { get; }

        /// <summary>
        /// Creates an <see cref="ISystemDataStream{TInstance}"/> for use in jobs.
        /// </summary>
        /// <param name="cancelRequestBehaviour">The type of <see cref="CancelRequestBehaviour"/> this stream should use.</param>
        /// <param name="uniqueContextIdentifier">An optional context string to uniquely identify this data stream on the system</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> in the stream.</typeparam>
        /// <returns>The <see cref="ISystemDataStream{TInstance}"/> instance</returns>
        public ISystemDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete, string uniqueContextIdentifier = null)
            where TInstance : unmanaged, IEntityKeyedTask;

        /// <summary>
        /// Creates an <see cref="ISystemEntityPersistentData{T}"/> instance that is bound to the lifecycle of the
        /// system instance.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IEntityPersistentDataInstance"/></typeparam>
        /// <returns>The <see cref="ISystemEntityPersistentData{T}"/> instance.</returns>
        public ISystemEntityPersistentData<T> CreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance;

        /// <summary>
        /// Configures an <see cref="ITaskUpdateJobForDefer{TInstance}"/> job to be run on the system. This will
        /// process the data to move it's progress forward and ultimately resolve into another data type.
        /// </summary>
        /// <param name="dataStream">The <see cref="ISystemDataStream{TInstance}"/> to schedule the job on.</param>
        /// <param name="scheduleJobFunction">The callback function to perform the scheduling</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for scheduling</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> in the stream</typeparam>
        /// <returns>
        /// A reference to the <see cref="IResolvableJobConfigRequirements"/> instance passed in to continue chaining
        /// configuration methods.
        /// </returns>
        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask;

        /// <summary>
        /// Configures an <see cref="ITaskCancelJobForDefer{TInstance}"/> job to be run on the system. This will operate
        /// on data in a stream that has been requested to cancel with <see cref="CancelRequestBehaviour.Unwind"/>. It
        /// provides the opportunity for the job to do the unwinding for however long that takes and to eventually
        /// resolve into a new data type and inherently notify of a <see cref="CancelComplete"/>.
        /// </summary>
        /// <param name="dataStream">The <see cref="ISystemDataStream{TInstance}"/> to schedule the job on.</param>
        /// <param name="scheduleJobFunction">The callback function to perform the scheduling</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for scheduling</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> in the stream</typeparam>
        /// <returns>
        /// A reference to the <see cref="IResolvableJobConfigRequirements"/> instance passed in to continue chaining
        /// configuration methods.
        /// </returns>
        public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask;

        /// <summary>
        /// Gets or Creates an <see cref="EntityQuery"/> tied to this system.
        /// </summary>
        /// <param name="componentTypes">The <see cref="ComponentType"/>s to construct the query.</param>
        /// <returns>The <see cref="EntityQuery"/> instance</returns>
        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes);
    }
}