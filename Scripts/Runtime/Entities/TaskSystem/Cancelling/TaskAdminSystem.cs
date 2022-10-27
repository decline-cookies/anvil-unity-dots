using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class TaskAdminSystem : AbstractAnvilSystemBase
    {
        private readonly List<AbstractTaskSystem> m_TaskSystemsToPropagateCancelFor;

        private readonly List<AbstractEntityProxyDataStream> m_WorldEntityProxyDataStreams;
        private readonly List<CancelRequestsDataStream> m_WorldCancelRequestsDataStreams;

        private BulkJobScheduler<CancelRequestsDataStream> m_WorldCancelRequestsBulkJobScheduler;
        private BulkJobScheduler<AbstractEntityProxyDataStream> m_WorldDataStreamBulkJobScheduler;
        
        public TaskAdminSystem()
        {
            m_TaskSystemsToPropagateCancelFor = new List<AbstractTaskSystem>();
        }

        public void RegisterTaskSystem(AbstractTaskSystem taskSystem)
        {
            m_TaskSystemsToPropagateCancelFor.Add(taskSystem);
            
            m_WorldEntityProxyDataStreams.AddRange(taskSystem.DataStreams);
            m_WorldCancelRequestsDataStreams.Add(taskSystem.CancelRequestsDataStream);
            foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
            {
                m_WorldEntityProxyDataStreams.AddRange(taskDriver.DataStreams);
                m_WorldCancelRequestsDataStreams.Add(taskDriver.CancelRequestsDataStream);
            }
        }

        public void Harden()
        {
            // m_WorldDataStreamBulkJobScheduler = new BulkJobScheduler<AbstractEntityProxyDataStream>(
        }

        protected override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;
            
            ///1. Propagate all Request Cancels
            ///2. Consolidate All EntityProxyData (this will write to PendingCancel)
            ///3. Consolidate All PendingCancelData

            foreach (AbstractTaskSystem taskSystem in m_TaskSystemsToPropagateCancelFor)
            {
                dependsOn = taskSystem.PropagateCancel(dependsOn);
            }

            foreach (AbstractTaskSystem taskSystem in m_TaskSystemsToPropagateCancelFor)
            {
                dependsOn = taskSystem.Consolidate(dependsOn);
            }

            Dependency = dependsOn;
        }
    }
}
