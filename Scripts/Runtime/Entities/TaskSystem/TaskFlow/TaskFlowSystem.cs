using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

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

        private BulkJobScheduler<CancelRequestDataStream> m_WorldCancelRequestsBulkJobScheduler;
        private BulkJobScheduler<TaskDriverCancelFlow> m_WorldCancelProgressBulkJobScheduler;

        private BulkJobScheduler<AbstractConsolidatableDataStream> m_WorldDataStreamBulkJobScheduler;
        private BulkJobScheduler<AbstractConsolidatableDataStream> m_WorldPendingCancelBulkJobScheduler;

        private bool m_HasInitialized;

        public TaskFlowSystem()
        {
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnDestroy()
        {
            //Clean up all the cached native arrays hidden in the schedulers
            m_WorldCancelRequestsBulkJobScheduler?.Dispose();
            m_WorldCancelProgressBulkJobScheduler?.Dispose();
            m_WorldDataStreamBulkJobScheduler?.Dispose();
            m_WorldPendingCancelBulkJobScheduler?.Dispose();

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
            m_WorldCancelRequestsBulkJobScheduler = TaskFlowGraph.CreateWorldCancelRequestsDataStreamBulkJobScheduler();
            m_WorldCancelProgressBulkJobScheduler = TaskFlowGraph.CreateWorldCancelFlowBulkJobScheduler();
            m_WorldDataStreamBulkJobScheduler = TaskFlowGraph.CreateWorldDataStreamBulkJobScheduler();
            m_WorldPendingCancelBulkJobScheduler = TaskFlowGraph.CreateWorldPendingCancelBulkJobScheduler();
        }

        protected override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;

            Debug.Log("Kicking off Consolidation for Task Flow System");

            //When someone has requested a cancel for a specific TaskDriver, that request is immediately propagated
            //down the entire chain to every Sub TaskDriver and their governing systems. So the first thing we need to
            //do is consolidate all the CancelRequestDataStreams so the lookups are all properly populated.
            dependsOn = m_WorldCancelRequestsBulkJobScheduler.Schedule(dependsOn,
                                                                       AbstractConsolidatableDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);
            
            //Next we check if any cancel progress was updated
            dependsOn = m_WorldCancelProgressBulkJobScheduler.Schedule(dependsOn,
                                                                       TaskDriverCancelFlow.SCHEDULE_FUNCTION);
            
            // Consolidate All EntityProxyDataStreams (this will check the lookups and write to PendingCancel)
            dependsOn = m_WorldDataStreamBulkJobScheduler.Schedule(dependsOn,
                                                                   AbstractConsolidatableDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);
            
            // Consolidate all PendingCancelDataStreams (Cancel jobs can run now)
            dependsOn = m_WorldPendingCancelBulkJobScheduler.Schedule(dependsOn,
                                                                      AbstractConsolidatableDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            // The Cancel Jobs will run later on in the frame and may have written that cancellation was completed to
            // the CancelCompletes. We'll consolidate those so that 
            //TODO: Consolidate the CancelCompletes


            Dependency = dependsOn;
        }
    }
}
