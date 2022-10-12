namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractNode
    {
        public TaskFlowGraph TaskFlowGraph
        {
            get;
        }
        
        public AbstractTaskSystem TaskSystem
        {
            get;
        }

        public AbstractTaskDriver TaskDriver
        {
            get;
        }

        protected AbstractNode(TaskFlowGraph taskFlowGraph, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            TaskFlowGraph = taskFlowGraph;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
        }
    }
}
