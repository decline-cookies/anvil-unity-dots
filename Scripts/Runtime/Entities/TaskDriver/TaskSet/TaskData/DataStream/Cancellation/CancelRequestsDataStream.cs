using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsDataStream : AbstractDataStream
    {
        private readonly CancelRequestsDataSource m_DataSource;
        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }
        public ActiveLookupData<EntityProxyInstanceID> ProgressLookupData { get; }

        public CancelRequestsDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelRequestsDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner);
            ProgressLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner);
        }

        public override uint GetActiveID()
        {
            return ActiveLookupData.ID;
        }
        
        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_DataSource.AcquirePendingAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_DataSource.ReleasePendingAsync(dependsOn);
        }

        public CancelRequestsWriter CreateCancelRequestsWriter()
        {
            return new CancelRequestsWriter(m_DataSource.PendingWriter, TaskSetOwner.TaskSet.CancelRequestsContexts);
        }
    }
}
