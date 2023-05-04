using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Helper class to wrap a <see cref="AbstractTaskDriverSystem"/> so that it has the context of the calling
    /// <see cref="AbstractTaskDriver"/>.
    /// </summary>
    /// <remarks>
    /// These functions just pipe directly to the system but handle passing along the context. This avoids errors of
    /// passing along the wrong context but also keeps the API the same as the Driver API.
    /// The consumer gets an <see cref="ITaskDriverSystem"/> to interface with and isn't aware of this wrapper.
    /// The <see cref="AbstractTaskDriverSystem"/> corresponding methods are marked internal so that they can't be
    /// accessed without going through here.
    /// </remarks>
    internal class ContextTaskDriverSystemWrapper : ITaskDriverSystem
    {
        private readonly AbstractTaskDriverSystem m_TaskDriverSystem;
        private readonly AbstractTaskDriver m_ContextTaskDriver;

        public ISystemCancelRequestDataStream CancelRequestDataStream
        {
            get => m_TaskDriverSystem.CancelRequestDataStream;
        }

        public ISystemDataStream<CancelComplete> CancelCompleteDataStream
        {
            get => m_TaskDriverSystem.CancelCompleteDataStream;
        }

        public ContextTaskDriverSystemWrapper(AbstractTaskDriverSystem taskDriverSystem, AbstractTaskDriver contextTaskDriver)
        {
            m_TaskDriverSystem = taskDriverSystem;
            m_ContextTaskDriver = contextTaskDriver;
        }

        public ISystemDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete, string uniqueContextIdentifier = null) 
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_TaskDriverSystem.CreateDataStream<TInstance>(m_ContextTaskDriver, cancelRequestBehaviour, uniqueContextIdentifier);
        }

        public ISystemEntityPersistentData<T> CreateEntityPersistentData<T>(string uniqueContextIdentifier) 
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return m_TaskDriverSystem.CreateEntityPersistentData<T>(uniqueContextIdentifier);
        }

        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(
            ISystemDataStream<TInstance> dataStream, 
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction, 
            BatchStrategy batchStrategy) 
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_TaskDriverSystem.ConfigureJobToUpdate(dataStream, scheduleJobFunction, batchStrategy);
        }

        public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(
            ISystemDataStream<TInstance> dataStream, 
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction, 
            BatchStrategy batchStrategy) 
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_TaskDriverSystem.ConfigureJobToCancel(dataStream, scheduleJobFunction, batchStrategy);
        }

        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
        {
            return m_TaskDriverSystem.GetEntityQuery(componentTypes);
        }

        public override string ToString()
        {
            return $"{m_TaskDriverSystem} via context {m_ContextTaskDriver}";
        }
    }
}
