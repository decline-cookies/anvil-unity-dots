namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsNode : AbstractNode
    {
        private readonly NodeLookup m_Lookup;
        
        public CancelRequestsDataStream CancelRequestsDataStream { get; }

        public CancelRequestsNode(NodeLookup lookup,
                                  CancelRequestsDataStream cancelRequestsDataStream,
                                  TaskFlowGraph taskFlowGraph,
                                  ITaskSystem taskSystem,
                                  ITaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            CancelRequestsDataStream = cancelRequestsDataStream;
        }

        protected override void DisposeSelf()
        {
            CancelRequestsDataStream.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{CancelRequestsDataStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
