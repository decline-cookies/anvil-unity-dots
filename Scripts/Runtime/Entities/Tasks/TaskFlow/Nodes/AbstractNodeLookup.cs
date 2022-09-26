using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractNodeLookup : AbstractAnvilBase
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
        
        protected AbstractNodeLookup(TaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            TaskFlowGraph = taskFlowGraph;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
        }
    }
}
