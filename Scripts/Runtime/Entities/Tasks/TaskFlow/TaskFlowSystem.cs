using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Data System (no update) for managing the world's <see cref="TaskFlowGraph"/>
    /// </summary>
    //TODO: Safer way to handle this. Discussion with Mike.
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class TaskFlowSystem : AbstractAnvilSystemBase
    {
        internal TaskFlowGraph TaskFlowGraph
        {
            get;
        }

        public TaskFlowSystem()
        {
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnUpdate()
        {
            TaskFlowGraph.Harden();
            Enabled = false;
        }
    }
}
