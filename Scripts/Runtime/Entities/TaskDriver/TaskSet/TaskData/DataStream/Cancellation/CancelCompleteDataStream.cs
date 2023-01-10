using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractDataStream
    {
        private readonly CancelCompleteDataSource m_DataSource;
        public ActiveArrayData<EntityProxyInstanceID> ActiveArrayData { get; }
        public UnsafeTypedStream<EntityProxyInstanceID>.Writer PendingWriter { get; }

        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelCompleteDataSource();
            PendingWriter = m_DataSource.PendingWriter;
            
            ActiveArrayData = m_DataSource.CreateActiveArrayData(TaskSetOwner, CancelBehaviour.None);
        }

        public override uint GetActiveID()
        {
            return ActiveArrayData.ID;
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
