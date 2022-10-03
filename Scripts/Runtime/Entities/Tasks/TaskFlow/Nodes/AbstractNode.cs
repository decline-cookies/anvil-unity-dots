using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractNode : AbstractAnvilBase
    {
        public TaskFlowGraph TaskFlowGraph
        {
            get;
        }
        
        public ITaskSystem TaskSystem
        {
            get;
        }

        public ITaskDriver TaskDriver
        {
            get;
        }

        protected AbstractNode(TaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            TaskFlowGraph = taskFlowGraph;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
        }
    }
}
