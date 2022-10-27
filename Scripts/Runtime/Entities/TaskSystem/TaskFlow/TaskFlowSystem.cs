using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// System to govern ownership of a <see cref="TaskFlowGraph"/> unique to a world.
    /// </summary>
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class TaskFlowSystem : AbstractAnvilSystemBase
    {
        internal TaskFlowGraph TaskFlowGraph
        {
            get;
        }

        private BulkJobScheduler<AbstractEntityProxyDataStream> m_WorldDataStreamBulkJobScheduler;
        private BulkJobScheduler<AbstractEntityProxyDataStream> m_WorldPendingCancelBulkJobScheduler;
        private BulkJobScheduler<TaskDriverCancellationPropagator> m_WorldTaskDriversCancellationBulkJobScheduler;
        private BulkJobScheduler<CancelRequestsDataStream> m_WorldCancelRequestsDataStreamBulkJobScheduler;
        private bool m_HasInitialized;

        public TaskFlowSystem()
        {
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnDestroy()
        {
            //Clean up all the cached native arrays hidden in the schedulers
            m_WorldDataStreamBulkJobScheduler?.Dispose();
            m_WorldPendingCancelBulkJobScheduler?.Dispose();
            m_WorldTaskDriversCancellationBulkJobScheduler?.Dispose();
            m_WorldCancelRequestsDataStreamBulkJobScheduler?.Dispose();
            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            //If for some reason this System gets re-enabled we don't want to initialize the graph anymore.
            if (m_HasInitialized)
            {
                return;
            }

            m_HasInitialized = true;

            TaskFlowGraph.ConfigureTaskSystemJobs();
            //TODO: #68 - Probably a better way to do this via a factory type. https://github.com/decline-cookies/anvil-unity-dots/pull/59#discussion_r977823711
            TaskFlowGraph.Harden();
            Harden();
        }

        private void Harden()
        {
            m_WorldDataStreamBulkJobScheduler = TaskFlowGraph.CreateWorldDataStreamBulkJobScheduler();
            m_WorldPendingCancelBulkJobScheduler = TaskFlowGraph.CreateWorldPendingCancelBulkJobScheduler();
            m_WorldTaskDriversCancellationBulkJobScheduler = TaskFlowGraph.CreateWorldTaskDriversCancellationBulkJobScheduler();
            m_WorldCancelRequestsDataStreamBulkJobScheduler = TaskFlowGraph.CreateWorldCancelRequestsDataStreamBulkJobScheduler();
        }

        protected override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;
            
            
            //TODO: Could we have it where upon writing the RequestCancel, we instantly write to all down the chain? Removing the need for a propagate step?
            
            // Propagates the request cancel to everyone who needs it
            dependsOn = m_WorldTaskDriversCancellationBulkJobScheduler.Schedule(dependsOn,
                                                                                TaskDriverCancellationPropagator.CONSOLIDATE_AND_PROPAGATE_SCHEDULE_FUNCTION);

            //Ensures that request cancel lookups are ready to go
            dependsOn = m_WorldCancelRequestsDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                                 CancelRequestsDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            // Consolidate All EntityProxyDataStreams (this will check the lookups and write to PendingCancel)
            dependsOn = m_WorldDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                   AbstractEntityProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            // Consolidate all PendingCancelDataStreams (Cancel jobs can run now)
            dependsOn = m_WorldPendingCancelBulkJobScheduler.Schedule(dependsOn,
                                                                      AbstractEntityProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);


            Dependency = dependsOn;
        }
    }
}
