namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsNode : AbstractNode
    {
        private readonly NodeLookup m_Lookup;
        
        public CancelRequestDataStream CancelRequestDataStream { get; }

        public CancelRequestsNode(NodeLookup lookup,
                                  CancelRequestDataStream cancelRequestDataStream,
                                  TaskFlowGraph taskFlowGraph,
                                  AbstractTaskDriverSystem taskSystem,
                                  AbstractTaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            CancelRequestDataStream = cancelRequestDataStream;
        }

        public override string ToString()
        {
            return $"{CancelRequestDataStream}";
        }
    }
}
