using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteDataStream : AbstractDataStream
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();

        private readonly CancelCompleteDataSource m_DataSource;
        
        //TODO: #137 - Gross, need to rearchitect DataStreams, better safety, expose only the Data
        
        public override uint ActiveID
        {
            get => ActiveArrayData.ID;
        }
        
        public ActiveArrayData<EntityProxyInstanceID> ActiveArrayData { get; }
        public PendingData<EntityProxyInstanceID> PendingData { get; }
        public UnsafeTypedStream<EntityProxyInstanceID>.Writer PendingWriter { get; }
        
        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelCompleteDataSource();
            PendingWriter = m_DataSource.PendingWriter;
            PendingData = m_DataSource.PendingData;
            
            ActiveArrayData = m_DataSource.CreateActiveArrayData(TaskSetOwner, CancelRequestBehaviour.Ignore);
            ScheduleInfo = ActiveArrayData.ScheduleInfo;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquireActiveAsync(AccessType accessType)
        {
            return ActiveArrayData.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseActiveAsync(JobHandle dependsOn)
        {
            ActiveArrayData.ReleaseAsync(dependsOn);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireActive(AccessType accessType)
        {
            ActiveArrayData.Acquire(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseActive()
        {
            ActiveArrayData.Release();
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
            m_DataSource.AcquirePendingAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePending()
        {
            m_DataSource.ReleasePending();
        }
        
        public CancelCompleteReader CreateCancelCompleteReader()
        {
            return new CancelCompleteReader(ActiveArrayData.DeferredJobArray);
        }
    }
}
