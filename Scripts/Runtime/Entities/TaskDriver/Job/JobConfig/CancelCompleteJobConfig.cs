namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteJobConfig : AbstractJobConfig
    {
        public CancelCompleteJobConfig(TaskFlowGraph taskFlowGraph,
                                       AbstractWorkload owningWorkload,
                                       CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskFlowGraph, owningWorkload)
        {
            RequireCancelCompleteDataStreamForRead(cancelCompleteDataStream);
        }
    }
}
