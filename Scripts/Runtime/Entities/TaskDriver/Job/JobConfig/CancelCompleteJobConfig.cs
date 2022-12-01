namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteJobConfig : AbstractJobConfig
    {
        public CancelCompleteJobConfig(TaskFlowGraph taskFlowGraph,
                                       AbstractTaskDriverSystem taskSystem,
                                       AbstractTaskDriver taskDriver,
                                       CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskFlowGraph, taskSystem, taskDriver)
        {
            RequireCancelCompleteDataStreamForRead(cancelCompleteDataStream);
        }
    }
}
