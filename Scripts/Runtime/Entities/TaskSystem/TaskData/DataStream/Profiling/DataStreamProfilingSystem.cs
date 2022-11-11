using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
#if DEBUG
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class DataStreamProfilingSystem : AbstractAnvilSystemBase
    {
        private readonly List<AbstractDataStream> m_AllDataStreams;
        
        private TaskFlowGraph m_TaskFlowGraph;
        private bool m_HasInitialized;

        public DataStreamProfilingSystem()
        {
            m_AllDataStreams = new List<AbstractDataStream>();
        }

        protected override void OnCreate()
        {
            m_TaskFlowGraph = World.GetExistingSystem<TaskFlowSystem>().TaskFlowGraph;
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            if (m_HasInitialized)
            {
                return;
            }

            m_HasInitialized = true;

            m_TaskFlowGraph.AddAllDataStreamsTo(m_AllDataStreams);
            
            base.OnStartRunning();
        }

        protected override void OnDestroy()
        {
           
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            foreach (AbstractDataStream dataStream in m_AllDataStreams)
            {
                dataStream.AccessController.Acquire(AccessType.SharedRead);
                
                dataStream.PopulateProfiler();
                
                dataStream.AccessController.Release();
            }
        }
    }
#endif
}
