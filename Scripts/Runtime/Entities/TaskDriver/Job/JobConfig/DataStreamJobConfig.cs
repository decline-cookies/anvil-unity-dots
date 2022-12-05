namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public DataStreamJobConfig(TaskFlowGraph taskFlowGraph,
                                   AbstractTaskSet owningTaskSet,
                                   DataStream<TInstance> dataStream)
            : base(taskFlowGraph,
                   owningTaskSet)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}
