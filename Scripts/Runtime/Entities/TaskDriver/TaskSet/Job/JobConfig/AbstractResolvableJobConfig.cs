using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractResolvableJobConfig : AbstractJobConfig,
                                                          IResolvableJobConfigRequirements
    {
        private static readonly MethodInfo PROTOTYPE_CREATE_RESOLVE_ACCESS_WRAPPER_METHOD = typeof(AbstractResolvableJobConfig).GetMethod(nameof(CreateAndAddDataStreamPendingAccessWrapperForResolving), BindingFlags.Instance | BindingFlags.NonPublic);

        private ResolveTargetTypeLookup m_ResolveTargetTypeLookup;

        private readonly HashSet<ResolveTargetDefinition> m_ResolveTargetDefinitions;

        protected AbstractResolvableJobConfig(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_ResolveTargetDefinitions = new HashSet<ResolveTargetDefinition>();
        }

        protected override void DisposeSelf()
        {
            m_ResolveTargetTypeLookup.Dispose();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        /// <inheritdoc cref="IResolvableJobConfigRequirements.RequireResolveTarget{TResolveTargetType}"/>
        public unsafe IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            TaskDriverManagementSystem taskDriverManagementSystem = TaskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            EntityProxyDataSource<TResolveTargetType> dataSource = taskDriverManagementSystem.GetOrCreateEntityProxyDataSource<TResolveTargetType>();
            
            m_ResolveTargetDefinitions.Add(ResolveTargetDefinition.Create<TResolveTargetType>(dataSource.PendingWriterPointer));
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
            m_ResolveTargetTypeLookup = new ResolveTargetTypeLookup(m_ResolveTargetDefinitions.Count);
            
            foreach (ResolveTargetDefinition targetDefinition in m_ResolveTargetDefinitions)
            {
                List<AbstractDataStream> resolvableDataStreams = new List<AbstractDataStream>();
                TaskSetOwner.AddResolvableDataStreamsTo(targetDefinition.Type, resolvableDataStreams);
                //TODO: Ensure there are values

                m_ResolveTargetTypeLookup.CreateWritersForType(targetDefinition, resolvableDataStreams);
                AddResolveAccessWrapper(targetDefinition.Type, resolvableDataStreams[0]);
            }
        }

        private void AddResolveAccessWrapper(Type type, AbstractDataStream dataStream)
        {
            MethodInfo genericCreateMethod = PROTOTYPE_CREATE_RESOLVE_ACCESS_WRAPPER_METHOD.MakeGenericMethod(type);
            genericCreateMethod.Invoke(this,
                                       new object[]
                                       {
                                           dataStream
                                       });
        }

        private void CreateAndAddDataStreamPendingAccessWrapperForResolving<TInstance>(AbstractDataStream dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            AddAccessWrapper(new DataStreamPendingAccessWrapper<TInstance>((EntityProxyDataStream<TInstance>)dataStream, AccessType.SharedWrite, Usage.Resolve));
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        
        /// <summary>
        /// Returns the job-safe struct of <see cref="DataStreamTargetResolver"/> so that jobs can
        /// resolve to the right <see cref="EntityProxyDataStream{TInstance}"/> based on context and id. 
        /// </summary>
        /// <returns>The <see cref="DataStreamTargetResolver"/> for this job config</returns>
        public ResolveTargetTypeLookup GetResolveTargetTypeLookup()
        {
            return m_ResolveTargetTypeLookup;
        }
    }
}
