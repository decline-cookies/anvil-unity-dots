using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractResolvableJobConfig : AbstractJobConfig,
                                                          IResolvableJobConfigRequirements
    {
        private readonly JobResolveTargetMapping m_JobResolveTargetMapping;
        private DataStreamTargetResolver m_DataStreamTargetResolver;

        protected AbstractResolvableJobConfig(TaskFlowGraph taskFlowGraph,
                                              AbstractTaskSystem taskSystem,
                                              AbstractTaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_JobResolveTargetMapping = new JobResolveTargetMapping();
        }

        protected override void DisposeSelf()
        {
            m_DataStreamTargetResolver.Dispose();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        /// <inheritdoc cref="IResolvableJobConfigRequirements.RequireResolveTarget{TResolveTargetType}"/>
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            //Any data streams that have registered for this resolve target type either on the system or related task drivers will be needed.
            //When the updater runs, it doesn't know yet which resolve target a particular instance will resolve to yet until it actually resolves.
            //We need to ensure that all possible locations have write access
            TaskFlowGraph.PopulateJobResolveTargetMappingForTarget<TResolveTargetType>(m_JobResolveTargetMapping, TaskSystem);
            
            if (m_JobResolveTargetMapping.Mapping.Count == 0)
            {
                return this;
            }
            
            List<ResolveTargetData> resolveTargetData = m_JobResolveTargetMapping.GetResolveTargetData<TResolveTargetType>();
            AddAccessWrapper(new JobConfigDataID(typeof(EntityProxyDataStream<TResolveTargetType>), Usage.Resolve),
                             DataStreamAsResolveTargetAccessWrapper.Create<TResolveTargetType>(resolveTargetData));

            return this;
        }

        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        protected sealed override void HardenConfig()
        {
            m_DataStreamTargetResolver = new DataStreamTargetResolver(m_JobResolveTargetMapping);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        
        /// <summary>
        /// Returns the job-safe struct of <see cref="DataStreamTargetResolver"/> so that jobs can
        /// resolve to the right <see cref="EntityProxyDataStream{TInstance}"/> based on context and id. 
        /// </summary>
        /// <returns>The <see cref="DataStreamTargetResolver"/> for this job config</returns>
        public DataStreamTargetResolver GetDataStreamChannelResolver()
        {
            return m_DataStreamTargetResolver;
        }
    }
}
