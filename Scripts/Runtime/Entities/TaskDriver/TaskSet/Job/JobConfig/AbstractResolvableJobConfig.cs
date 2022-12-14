using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractResolvableJobConfig : AbstractJobConfig
    {
        // private readonly JobResolveTargetMapping m_JobResolveTargetMapping;
        // private DataStreamTargetResolver m_DataStreamTargetResolver;

        private readonly HashSet<Type> m_ResolveTypes;

        protected AbstractResolvableJobConfig(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_ResolveTypes = new HashSet<Type>();
        }

        protected override void DisposeSelf()
        {
            // m_DataStreamTargetResolver.Dispose();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        /// <inheritdoc cref="IResolvableJobConfigRequirements.RequireResolveTarget{TResolveTargetType}"/>
        public IJobConfig RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            //TODO: We're not necessarily hardened yet so we should just store the type for now, we'll add the access wrappers later during hardening
            m_ResolveTypes.Add(typeof(TResolveTargetType));
            return this;
            
            // //Any data streams that have registered for this resolve target type either on the system or related task drivers will be needed.
            // //When the updater runs, it doesn't know yet which resolve target a particular instance will resolve to yet until it actually resolves.
            // //We need to ensure that all possible locations have write access
            // TaskFlowGraph.PopulateJobResolveTargetMappingForTarget<TResolveTargetType>(m_JobResolveTargetMapping, OwningTaskSet.CommonTaskSet);
            //
            // if (m_JobResolveTargetMapping.Mapping.Count == 0)
            // {
            //     return this;
            // }
            //
            // List<ResolveTargetData> resolveTargetData = m_JobResolveTargetMapping.GetResolveTargetData<TResolveTargetType>();
            // AddAccessWrapper(new DataStreamAsResolveTargetAccessWrapper<TResolveTargetType>(resolveTargetData.ToArray(), Usage.Resolve));
            //
            // return this;
        }

        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        protected sealed override void HardenConfig()
        {
            //TODO: Get all the TaskDrivers and System to find the DataStreams that match the types. 
            //TODO: Then build the lookup. For a given Type resolve the to TypeID.
            //TODO: That TypeID should have a lookup for TaskDriverID.
            //TODO: That TaskDriverID should have a link to the Pending Writer Pointer and the ActiveID for consolidating to.
            
            // m_DataStreamTargetResolver = new DataStreamTargetResolver(m_JobResolveTargetMapping);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        
        /// <summary>
        // /// Returns the job-safe struct of <see cref="DataStreamTargetResolver"/> so that jobs can
        // /// resolve to the right <see cref="DataStream{TInstance}"/> based on context and id. 
        // /// </summary>
        // /// <returns>The <see cref="DataStreamTargetResolver"/> for this job config</returns>
        // public DataStreamTargetResolver GetDataStreamTargetResolver()
        // {
        //     return m_DataStreamTargetResolver;
        // }
    }
}
