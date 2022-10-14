namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsNode : AbstractNode
    {
        private readonly NodeLookup m_Lookup;
        
        public CancelRequestsDataStream CancelRequestsDataStream { get; }

        public CancelRequestsNode(NodeLookup lookup,
                                  CancelRequestsDataStream cancelRequestsDataStream,
                                  TaskFlowGraph taskFlowGraph,
                                  AbstractTaskSystem taskSystem,
                                  AbstractTaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            CancelRequestsDataStream = cancelRequestsDataStream;
        }

        public override string ToString()
        {
            return $"{CancelRequestsDataStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
