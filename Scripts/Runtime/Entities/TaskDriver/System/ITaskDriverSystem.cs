using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    public interface ITaskDriverSystem
    {
        /// <summary>
        /// Data Stream representing requests to Cancel an <see cref="Entity"/>
        /// </summary>
        public ISystemCancelRequestDataStream CancelRequestDataStream { get; }
        
        /// <summary>
        /// Data Stream representing when Cancel Requests are Complete
        /// </summary>
        public ISystemDataStream<CancelComplete> CancelCompleteDataStream { get; }
        
        public ISystemDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete)
            where TInstance : unmanaged, IEntityProxyInstance;

        public ISystemEntityPersistentData<T> CreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance;

        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance;

        public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(
            ISystemDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance;

        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes);
    }
}
