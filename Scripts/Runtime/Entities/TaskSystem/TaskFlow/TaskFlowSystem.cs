using Unity.Entities;

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

        private bool m_HasInitialized;

        public TaskFlowSystem()
        {
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            //If for some reason this System gets re-enabled we don't want to initialize the graph anymore.
            if (m_HasInitialized)
            {
                Enabled = false;
                return;
            }
            m_HasInitialized = true;
            
            TaskFlowGraph.ConfigureTaskSystemJobs();
            //TODO: #68 - Probably a better way to do this via a factory type. https://github.com/decline-cookies/anvil-unity-dots/pull/59#discussion_r977823711
            TaskFlowGraph.Harden();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }
    }
}
