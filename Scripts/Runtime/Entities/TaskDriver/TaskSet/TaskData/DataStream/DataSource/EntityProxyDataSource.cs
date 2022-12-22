using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityProxyDataSource<TInstance> : AbstractDataSource<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private EntityProxyDataSourceConsolidator<TInstance> m_EntityProxyDataSourceConsolidator;

        protected override void DisposeSelf()
        {
            m_EntityProxyDataSourceConsolidator.Dispose();
            base.DisposeSelf();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();
            m_EntityProxyDataSourceConsolidator = new EntityProxyDataSourceConsolidator<TInstance>(PendingData, ActiveDataLookupByID);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateEntityProxyDataSourceJob consolidateEntityProxyDataSourceJob = new ConsolidateEntityProxyDataSourceJob(m_EntityProxyDataSourceConsolidator);
            dependsOn = consolidateEntityProxyDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
        [BurstCompile]
        private struct ConsolidateEntityProxyDataSourceJob : IJob
        {
            private EntityProxyDataSourceConsolidator<TInstance> m_EntityProxyDataSourceConsolidator;
        
            public ConsolidateEntityProxyDataSourceJob(EntityProxyDataSourceConsolidator<TInstance> dataSourceConsolidator) : this()
            {
                m_EntityProxyDataSourceConsolidator = dataSourceConsolidator;
            }
        
            public void Execute()
            {
                m_EntityProxyDataSourceConsolidator.Consolidate();
            }
        }
    }
}
