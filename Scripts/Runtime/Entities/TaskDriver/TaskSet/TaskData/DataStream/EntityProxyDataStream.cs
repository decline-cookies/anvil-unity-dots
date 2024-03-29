using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #137 - Too much complexity that is not needed
    internal class EntityProxyDataStream<TInstance> : AbstractDataStream,
                                                      IDriverDataStream<TInstance>,
                                                      ISystemDataStream<TInstance>,
                                                      ICancellableDataStream
        where TInstance : unmanaged, IEntityKeyedTask
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityKeyedTaskWrapper<TInstance>>();

        private readonly EntityProxyDataSource<TInstance> m_DataSource;
        private readonly ActiveArrayData<EntityKeyedTaskWrapper<TInstance>> m_ActiveArrayData;
        private readonly ActiveArrayData<EntityKeyedTaskWrapper<TInstance>> m_ActiveCancelArrayData;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }
        public DeferredNativeArrayScheduleInfo ActiveCancelScheduleInfo { get; }

        public override DataTargetID DataTargetID
        {
            get => m_ActiveArrayData.WorldUniqueID;
        }

        public override IDataSource DataSource
        {
            get => m_DataSource;
        }

        public DataTargetID CancelDataTargetID
        {
            get => m_ActiveCancelArrayData.WorldUniqueID;
        }

        public Type InstanceType { get; }

        public CancelRequestBehaviour CancelBehaviour
        {
            get => m_ActiveArrayData.CancelRequestBehaviour;
        }

        public uint ActiveDataVersion
        {
            get => m_ActiveArrayData.Version;
        }

        public uint ActiveCancelDataVersion
        {
            get => m_ActiveCancelArrayData.Version;
        }

        //TODO: #136 - Not good to expose these just for the CancelComplete case.
        public UnsafeTypedStream<EntityKeyedTaskWrapper<TInstance>>.Writer PendingWriter { get; }
        public PendingData<EntityKeyedTaskWrapper<TInstance>> PendingData { get; }

        public EntityProxyDataStream(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour, string uniqueContextIdentifier)
            : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystemManaged<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetOrCreateEntityProxyDataSource<TInstance>();

            m_ActiveArrayData = m_DataSource.CreateActiveArrayData(taskSetOwner, cancelRequestBehaviour, uniqueContextIdentifier);

            if (m_ActiveArrayData.ActiveCancelData != null)
            {
                m_ActiveCancelArrayData
                    = (ActiveArrayData<EntityKeyedTaskWrapper<TInstance>>)m_ActiveArrayData.ActiveCancelData;
                ActiveCancelScheduleInfo = m_ActiveCancelArrayData.ScheduleInfo;
            }

            ScheduleInfo = m_ActiveArrayData.ScheduleInfo;

            InstanceType = typeof(TInstance);
        }

        public EntityProxyDataStream(AbstractTaskDriver taskDriver, EntityProxyDataStream<TInstance> systemDataStream)
            : base(taskDriver)
        {
            m_DataSource = systemDataStream.m_DataSource;
            m_ActiveArrayData = systemDataStream.m_ActiveArrayData;
            ScheduleInfo = systemDataStream.ScheduleInfo;
            ActiveCancelScheduleInfo = systemDataStream.ActiveCancelScheduleInfo;
            m_ActiveCancelArrayData = systemDataStream.m_ActiveCancelArrayData;

            //TODO: #136 - Not good to expose these just for the CancelComplete case.
            PendingData = systemDataStream.PendingData;
            PendingWriter = systemDataStream.PendingWriter;

            InstanceType = typeof(TInstance);
        }

        //TODO: #137 - Gross!!! This is a special case only for CancelComplete
        protected EntityProxyDataStream(ITaskSetOwner taskSetOwner, string uniqueContextIdentifier) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystemManaged<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelCompleteDataSource() as EntityProxyDataSource<TInstance>;
            m_ActiveArrayData = m_DataSource.CreateActiveArrayData(taskSetOwner, CancelRequestBehaviour.Ignore, uniqueContextIdentifier);
            ScheduleInfo = m_ActiveArrayData.ScheduleInfo;

            //TODO: #136 - Not good to expose these just for the CancelComplete case.
            PendingWriter = m_DataSource.PendingWriter;
            PendingData = m_DataSource.PendingData;

            InstanceType = typeof(TInstance);
        }

        public bool IsActiveDataInvalidated(uint lastVersion)
        {
            return m_ActiveArrayData.IsDataInvalidated(lastVersion);
        }

        public bool IsActiveCancelDataInvalidated(uint lastVersion)
        {
            return m_ActiveCancelArrayData.IsDataInvalidated(lastVersion);
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
        public JobHandle AcquireActiveCancelAsync(AccessType accessType)
        {
            return m_ActiveArrayData.ActiveCancelData.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseActiveCancelAsync(JobHandle dependsOn)
        {
            m_ActiveArrayData.ActiveCancelData.ReleaseAsync(dependsOn);
        }


        public DataStreamPendingWriter<TInstance> CreateDataStreamPendingWriter()
        {
            return new DataStreamPendingWriter<TInstance>(m_DataSource.PendingWriter, TaskSetOwner.WorldUniqueID, m_ActiveArrayData.WorldUniqueID);
        }

        public DataStreamActiveReader<TInstance> CreateDataStreamActiveReader()
        {
            // A deferred array is only required if we're still waiting on the data to be readable.
            // If our read job handle is ready now then the data is too and we should read from the current array.
            // Using the deferred array would produce invalid results because the deferred array gets resolved when the
            // data is written and in this case the writing is complete.
            bool isDeferredRequired = !m_ActiveArrayData.GetDependencyFor(AccessType.SharedRead).IsCompleted;
            NativeArray<EntityKeyedTaskWrapper<TInstance>> sourceArray
                = isDeferredRequired ? m_ActiveArrayData.DeferredJobArray : m_ActiveArrayData.CurrentArray;
            return new DataStreamActiveReader<TInstance>(sourceArray);
        }

        public DataStreamUpdater<TInstance> CreateDataStreamUpdater(ResolveTargetTypeLookup resolveTargetTypeLookup)
        {
            return new DataStreamUpdater<TInstance>(
                m_DataSource.PendingWriter,
                m_ActiveArrayData.DeferredJobArray,
                resolveTargetTypeLookup);
        }

        public DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(
            ResolveTargetTypeLookup resolveTargetTypeLookup,
            UnsafeParallelHashMap<EntityKeyedTaskID, bool> cancelProgressLookup)
        {
            return new DataStreamCancellationUpdater<TInstance>(
                m_DataSource.PendingWriter,
                m_ActiveCancelArrayData.DeferredJobArray,
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