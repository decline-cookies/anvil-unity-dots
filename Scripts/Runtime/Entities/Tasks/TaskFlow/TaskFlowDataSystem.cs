namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Data System (no update) for managing the world's <see cref="TaskFlowGraph"/>
    /// </summary>
    public partial class TaskFlowDataSystem : AbstractDataSystem
    {
        internal TaskFlowGraph TaskFlowGraph
        {
            get;
        }

        public TaskFlowDataSystem()
        {
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            TaskFlowGraph.Harden();
        }
    }
}
