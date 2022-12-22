namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public DataStreamJobConfig(ITaskSetOwner taskSetOwner,
                                   EntityProxyDataStream<TInstance> dataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}
