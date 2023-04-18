using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractResolvableJobConfig : AbstractJobConfig, IResolvableJobConfigRequirements
    {
        private static readonly MethodInfo PROTOTYPE_CREATE_RESOLVE_ACCESS_WRAPPER_METHOD
            = typeof(AbstractResolvableJobConfig).GetMethod(
                nameof(CreateAndAddDataStreamPendingAccessWrapperForResolving),
                BindingFlags.Instance | BindingFlags.NonPublic);

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
        }

        public IResolvableJobConfigRequirements AddRequirementsFrom<T>(T taskDriver, IResolvableJobConfigRequirements.ConfigureJobRequirementsDelegate<T> configureRequirements)
            where T : AbstractTaskDriver
        {
            return configureRequirements(taskDriver, this);
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
                Debug_EnsureValuesArePresent(resolvableDataStreams);
                m_ResolveTargetTypeLookup.CreateWritersForType(targetDefinition, resolvableDataStreams);
                //TODO: #136 - Gross to access first element, we should be able to get direct access to the PendingData
                AddResolveAccessWrapper(targetDefinition.Type, resolvableDataStreams[0]);
            }
        }

        private void AddResolveAccessWrapper(Type type, AbstractDataStream dataStream)
        {
            MethodInfo genericCreateMethod = PROTOTYPE_CREATE_RESOLVE_ACCESS_WRAPPER_METHOD.MakeGenericMethod(type);
            genericCreateMethod.Invoke(this, new object[] { dataStream });
        }

        private void CreateAndAddDataStreamPendingAccessWrapperForResolving<TInstance>(AbstractDataStream dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            AddAccessWrapper(
                new DataStreamPendingAccessWrapper<TInstance>(
                    (EntityProxyDataStream<TInstance>)dataStream,
                    AccessType.SharedWrite,
                    Usage.Resolve));
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        /// <summary>
        /// Returns the job-safe struct of <see cref="ResolveTargetTypeLookup"/> so that jobs can
        /// resolve to the right <see cref="EntityProxyDataStream{TInstance}"/> based on context and id.
        /// </summary>
        /// <returns>The <see cref="ResolveTargetTypeLookup"/> for this job config</returns>
        public ResolveTargetTypeLookup GetResolveTargetTypeLookup()
        {
            return m_ResolveTargetTypeLookup;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureValuesArePresent(List<AbstractDataStream> dataStreams)
        {
            if (dataStreams.Count <= 0)
            {
                throw new InvalidOperationException("Trying to get writers for resolvable data streams but none are present!");
            }
        }
    }
}