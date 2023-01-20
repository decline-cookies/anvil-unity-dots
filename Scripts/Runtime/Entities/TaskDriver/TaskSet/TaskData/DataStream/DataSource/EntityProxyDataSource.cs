using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityProxyDataSource<TInstance> : AbstractDataSource<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

        public EntityProxyDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem)
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
            
            //We need to ensure we get the right access to any of the cancel data structures
            foreach (AbstractData data in ActiveDataLookupByID.Values)
            {
                //If this piece of data can be cancelled, we need to be able to read the associated Cancel Request lookup
                if (data.CancelBehaviour is CancelBehaviour.Default or CancelBehaviour.Explicit)
                {
                    AddConsolidationData(data.TaskSetOwner.TaskSet.CancelRequestsDataStream.ActiveLookupData, AccessType.SharedRead);
                }
            }

            m_Consolidator = new EntityProxyDataSourceConsolidator<TInstance>(PendingData, ActiveDataLookupByID);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateEntityProxyDataSourceJob consolidateEntityProxyDataSourceJob = new ConsolidateEntityProxyDataSourceJob(m_Consolidator);
            dependsOn = consolidateEntityProxyDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
        [BurstCompile]
        private struct ConsolidateEntityProxyDataSourceJob : IJob
        {
            private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;
        
            public ConsolidateEntityProxyDataSourceJob(EntityProxyDataSourceConsolidator<TInstance> dataSourceConsolidator) : this()
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
