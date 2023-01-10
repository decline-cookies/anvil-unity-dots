using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractDataStream
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();

        private readonly CancelCompleteDataSource m_DataSource;
        public ActiveArrayData<EntityProxyInstanceID> ActiveArrayData { get; }
        public UnsafeTypedStream<EntityProxyInstanceID>.Writer PendingWriter { get; }
        
        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelCompleteDataSource();
            PendingWriter = m_DataSource.PendingWriter;
            
            ActiveArrayData = m_DataSource.CreateActiveArrayData(TaskSetOwner, CancelBehaviour.None);
            ScheduleInfo = ActiveArrayData.ScheduleInfo;
        }

        public override uint GetActiveID()
        {
            return ActiveArrayData.ID;
        }
        
        public JobHandle AcquireActiveAsync(AccessType accessType)
        {
            return ActiveArrayData.AcquireAsync(accessType);
        }

        public void ReleaseActiveAsync(JobHandle dependsOn)
        {
            ActiveArrayData.ReleaseAsync(dependsOn);
        }
        
        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_DataSource.AcquirePendingAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_DataSource.ReleasePendingAsync(dependsOn);
        }
        
        public CancelCompleteReader CreateCancelCompleteReader()
        {
            return new CancelCompleteReader(ActiveArrayData.DeferredJobArray);
        }
    }
}
