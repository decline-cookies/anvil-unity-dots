using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
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

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }
        public DeferredNativeArrayScheduleInfo PendingCancelScheduleInfo { get; }

        public override uint ActiveID
        {
            get => m_ActiveArrayData.ID;
        }

        //TODO: #136 - Not good to expose these just for the CancelComplete case.
        public UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer PendingWriter { get; }
        public PendingData<EntityProxyInstanceWrapper<TInstance>> PendingData { get; }

        public EntityProxyDataStream(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour) : base(taskSetOwner)
        {
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

            //TODO: #136 - Not good to expose these just for the CancelComplete case.
            PendingData = systemDataStream.PendingData;
            PendingWriter = systemDataStream.PendingWriter;
        }

        //TODO: #137 - Gross!!! This is a special case only for CancelComplete
        protected EntityProxyDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelCompleteDataSource() as EntityProxyDataSource<TInstance>;
            m_ActiveArrayData = m_DataSource.CreateActiveArrayData(taskSetOwner, CancelRequestBehaviour.Ignore);
            ScheduleInfo = m_ActiveArrayData.ScheduleInfo;

            //TODO: #136 - Not good to expose these just for the CancelComplete case.
            PendingWriter = m_DataSource.PendingWriter;
            PendingData = m_DataSource.PendingData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_DataSource.AcquirePendingAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_DataSource.ReleasePendingAsync(dependsOn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquirePending(AccessType accessType)
        {
            m_DataSource.AcquirePending(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePending()
        {
            m_DataSource.ReleasePending();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquireActiveAsync(AccessType accessType)
        {
            return m_ActiveArrayData.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseActiveAsync(JobHandle dependsOn)
        {
            m_ActiveArrayData.ReleaseAsync(dependsOn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireActive(AccessType accessType)
        {
            m_ActiveArrayData.Acquire(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseActive()
        {
            m_ActiveArrayData.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquirePendingCancelActiveAsync(AccessType accessType)
        {
            return m_ActiveArrayData.PendingCancelActiveData.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        //*************************************************************************************************************
        // IABSTRACT DATA STREAM INTERFACE
        //*************************************************************************************************************

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.AcquireActiveReaderAsync"/>
        public JobHandle AcquireActiveReaderAsync(out DataStreamActiveReader<TInstance> reader)
        {
            JobHandle dependsOn = AcquireActiveAsync(AccessType.SharedRead);
            reader = CreateDataStreamActiveReader();
            return dependsOn;
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.ReleaseActiveReaderAsync"/>
        public void ReleaseActiveReaderAsync(JobHandle dependsOn)
        {
            ReleaseActiveAsync(dependsOn);
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.AcquireActiveReader"/>
        public DataStreamActiveReader<TInstance> AcquireActiveReader()
        {
            AcquireActive(AccessType.SharedRead);
            return CreateDataStreamActiveReader();
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.ReleaseActiveReader"/>
        public void ReleaseActiveReader()
        {
            ReleaseActive();
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.AcquirePendingWriterAsync"/>
        public JobHandle AcquirePendingWriterAsync(out DataStreamPendingWriter<TInstance> writer)
        {
            JobHandle dependsOn = AcquirePendingAsync(AccessType.SharedWrite);
            writer = CreateDataStreamPendingWriter();
            return dependsOn;
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.ReleasePendingWriterAsync"/>
        public void ReleasePendingWriterAsync(JobHandle dependsOn)
        {
            ReleasePendingAsync(dependsOn);
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.AcquirePendingWriter"/>
        public DataStreamPendingWriter<TInstance> AcquirePendingWriter()
        {
            AcquirePending(AccessType.SharedWrite);
            return CreateDataStreamPendingWriter();
        }

        /// <inheritdoc cref="IAbstractDataStream{TInstance}.ReleasePendingWriter"/>
        public void ReleasePendingWriter()
        {
            ReleasePending();
        }
    }
}
