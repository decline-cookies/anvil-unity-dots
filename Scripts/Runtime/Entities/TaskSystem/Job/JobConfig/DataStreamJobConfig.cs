namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public DataStreamJobConfig(TaskFlowGraph taskFlowGraph,
                                   AbstractTaskSystem taskSystem,
                                   AbstractTaskDriver taskDriver,
                                   EntityProxyDataStream<TInstance> dataStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}
