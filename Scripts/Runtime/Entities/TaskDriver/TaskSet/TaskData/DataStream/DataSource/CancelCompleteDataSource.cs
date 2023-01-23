using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteDataSource : AbstractDataSource<EntityProxyInstanceID>
    {
        private CancelCompleteDataSourceConsolidator m_Consolidator;
        public CancelCompleteDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem)
        {
        }

        protected override void DisposeSelf()
        {
            m_Consolidator.Dispose();
            base.DisposeSelf();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();
            m_Consolidator = new CancelCompleteDataSourceConsolidator(PendingData, ActiveDataLookupByID);
        }

        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateCancelCompleteDataSourceJob consolidateCancelCompleteDataSourceJob = new ConsolidateCancelCompleteDataSourceJob(m_Consolidator);
            dependsOn = consolidateCancelCompleteDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
        [BurstCompile]
        private struct ConsolidateCancelCompleteDataSourceJob : IJob
        {
            private CancelCompleteDataSourceConsolidator m_Consolidator;
        
            public ConsolidateCancelCompleteDataSourceJob(CancelCompleteDataSourceConsolidator dataSourceConsolidator) : this()
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
