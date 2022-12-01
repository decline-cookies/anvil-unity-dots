namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public DataStreamJobConfig(TaskFlowGraph taskFlowGraph,
                                   AbstractWorkload owningWorkload,
                                   DataStream<TInstance> dataStream)
            : base(taskFlowGraph,
                   owningWorkload)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}
