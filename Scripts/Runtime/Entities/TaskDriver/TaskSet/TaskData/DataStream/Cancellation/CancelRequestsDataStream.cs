using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsDataStream : AbstractDataStream
    {
        private readonly CancelRequestsDataSource m_DataSource;
        private readonly ActiveLookupData<EntityProxyInstanceID> m_ActiveLookupData;

        public CancelRequestsDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelRequestsDataSource();

            m_ActiveLookupData = m_DataSource.CreateActiveLookupData();
        }

        public override uint GetActiveID()
        {
            return m_ActiveLookupData.ID;
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
