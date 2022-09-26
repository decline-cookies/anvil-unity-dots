using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractNodeLookup : AbstractAnvilBase
    {
        
        public TaskFlowGraph TaskGraph
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

        protected AbstractNodeLookup(TaskFlowGraph taskGraph, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            TaskGraph = taskGraph;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
        }
    }
}
