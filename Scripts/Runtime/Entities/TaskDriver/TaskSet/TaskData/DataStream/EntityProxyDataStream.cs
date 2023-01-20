using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #137 - Too much complexity that is not needed
    internal class EntityProxyDataStream<TInstance> : AbstractDataStream,
                                                      IDriverDataStream<TInstance>,
                                                      ISystemDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();

        private readonly EntityProxyDataSource<TInstance> m_DataSource;
        private readonly ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> m_ActiveArrayData;
        private readonly ActiveArrayData<EntityProxyInstanceWrapper<TInstance>> m_PendingCancelActiveArrayData;
        private readonly CancelRequestBehaviour m_CancelRequestBehaviour;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }
        public DeferredNativeArrayScheduleInfo PendingCancelScheduleInfo { get; }
        
        public override uint ActiveID
        {
            get => m_ActiveArrayData.ID;
        }
        
        public EntityProxyDataStream(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour) : base(taskSetOwner)
        {
            m_CancelRequestBehaviour = cancelRequestBehaviour;
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetOrCreateEntityProxyDataSource<TInstance>();

            m_ActiveArrayData = m_DataSource.CreateActiveArrayData(taskSetOwner, cancelRequestBehaviour);

            if (m_ActiveArrayData.PendingCancelActiveData != null)
            {
                m_PendingCancelActiveArrayData = (ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>)m_ActiveArrayData.PendingCancelActiveData;
                PendingCancelScheduleInfo = m_PendingCancelActiveArrayData.ScheduleInfo;
            }

            ScheduleInfo = m_ActiveArrayData.ScheduleInfo;
        }

        public EntityProxyDataStream(AbstractTaskDriver taskDriver, EntityProxyDataStream<TInstance> systemDataStream) : base(taskDriver)
        {
            m_DataSource = systemDataStream.m_DataSource;
            m_ActiveArrayData = systemDataStream.m_ActiveArrayData;
            ScheduleInfo = systemDataStream.ScheduleInfo;
            PendingCancelScheduleInfo = systemDataStream.PendingCancelScheduleInfo;
            m_PendingCancelActiveArrayData = systemDataStream.m_PendingCancelActiveArrayData;
        }

        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_DataSource.AcquirePendingAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_DataSource.ReleasePendingAsync(dependsOn);
        }

        public JobHandle AcquireActiveAsync(AccessType accessType)
        {
            return m_ActiveArrayData.AcquireAsync(accessType);
        }

        public void ReleaseActiveAsync(JobHandle dependsOn)
        {
            m_ActiveArrayData.ReleaseAsync(dependsOn);
        }

        public JobHandle AcquirePendingCancelActiveAsync(AccessType accessType)
        {
            return m_ActiveArrayData.PendingCancelActiveData.AcquireAsync(accessType);
        }

        public void ReleasePendingCancelActiveAsync(JobHandle dependsOn)
        {
            m_ActiveArrayData.PendingCancelActiveData.ReleaseAsync(dependsOn);
        }


        public DataStreamPendingWriter<TInstance> CreateDataStreamPendingWriter()
        {
            return new DataStreamPendingWriter<TInstance>(m_DataSource.PendingWriter, TaskSetOwner.ID, m_ActiveArrayData.ID);
        }

        public DataStreamActiveReader<TInstance> CreateDataStreamActiveReader()
        {
            return new DataStreamActiveReader<TInstance>(m_ActiveArrayData.DeferredJobArray);
        }

        public DataStreamUpdater<TInstance> CreateDataStreamUpdater(ResolveTargetTypeLookup resolveTargetTypeLookup)
        {
            return new DataStreamUpdater<TInstance>(m_DataSource.PendingWriter,
                                                    m_ActiveArrayData.DeferredJobArray,
                                                    resolveTargetTypeLookup);
        }

        public DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(ResolveTargetTypeLookup resolveTargetTypeLookup,
                                                                                            UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelProgressLookup)
        {
            return new DataStreamCancellationUpdater<TInstance>(m_DataSource.PendingWriter,
                                                                m_PendingCancelActiveArrayData.DeferredJobArray,
                                                                resolveTargetTypeLookup,
                                                                cancelProgressLookup);
        }
    }
}
