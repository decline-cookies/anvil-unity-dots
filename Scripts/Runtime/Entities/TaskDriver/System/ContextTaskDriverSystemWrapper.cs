using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
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

        public ISystemDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete) 
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return m_TaskDriverSystem.CreateDataStream<TInstance>(m_ContextTaskDriver, cancelRequestBehaviour);
        }

        public ISystemEntityPersistentData<T> CreateEntityPersistentData<T>() 
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return m_TaskDriverSystem.CreateEntityPersistentData<T>();
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
