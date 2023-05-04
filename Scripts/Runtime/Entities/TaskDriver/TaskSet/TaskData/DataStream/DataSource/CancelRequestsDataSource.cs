using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelRequestsDataSource : AbstractEntityProxyInstanceIDDataSource
    {
        private CancelRequestsDataSourceConsolidator m_Consolidator;

        public CancelRequestsDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem, "CANCEL_REQUEST") { }

        protected override void DisposeSelf()
        {
            m_Consolidator.Dispose();
            base.DisposeSelf();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();
            //We'll want to write to the Cancel Complete collection directly if we don't have to wait for explicit cancel jobs so we need SharedWrite Access
            AddConsolidationData(TaskDriverManagementSystem.GetCancelCompleteDataSource().PendingData, AccessType.SharedWrite);

            //For each piece of data we want to get exclusive write access to the Progress lookup as well
            foreach (AbstractData data in ActiveDataLookupByDataTargetID.Values)
            {
                ActiveLookupData<EntityProxyInstanceID> progressLookupData = data.TaskSetOwner.TaskSet.CancelProgressDataStream.ActiveLookupData;
                AddConsolidationData(progressLookupData, AccessType.ExclusiveWrite);
            }

            m_Consolidator = new CancelRequestsDataSourceConsolidator(PendingData, ActiveDataLookupByDataTargetID);
        }

        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateCancelRequestsDataSourceJob consolidateCancelRequestsDataSourceJob = new ConsolidateCancelRequestsDataSourceJob(m_Consolidator);
            dependsOn = consolidateCancelRequestsDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelRequestsDataSourceJob : IJob
        {
            private CancelRequestsDataSourceConsolidator m_Consolidator;

            public ConsolidateCancelRequestsDataSourceJob(CancelRequestsDataSourceConsolidator dataSourceConsolidator) : this()
            {
                m_Consolidator = dataSourceConsolidator;
            }

            public void Execute()
            {
                m_Consolidator.Consolidate();
            }
        }
    }
}
