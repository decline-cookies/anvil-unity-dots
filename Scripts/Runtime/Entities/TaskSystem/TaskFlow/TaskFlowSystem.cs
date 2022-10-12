using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// System to govern ownership of a <see cref="TaskFlowGraph"/> unique to a world.
    /// </summary>
    //TODO: !!!!!!!
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class TaskFlowSystem : AbstractAnvilSystemBase
    {
        internal TaskFlowGraph TaskFlowGraph
        {
            get;
        }

        public TaskFlowSystem()
        {
            Logger.Debug("Task Flow System Constructor");
            TaskFlowGraph = new TaskFlowGraph();
        }

        protected override void OnCreate()
        {
            Logger.Debug("Task Flow System OnCreate");
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            Logger.Debug("Task Flow System OnStartRunning");
            base.OnStartRunning();

            //TODO: #68 - Probably a better way to do this via a factory type. https://github.com/decline-cookies/anvil-unity-dots/pull/59#discussion_r977823711
            TaskFlowGraph.Harden();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            Logger.Debug("Task Flow System OnUpdate");
        }
    }
}
