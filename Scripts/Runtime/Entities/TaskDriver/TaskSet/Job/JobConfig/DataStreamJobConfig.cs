namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public DataStreamJobConfig(ITaskSetOwner taskSetOwner,
                                   DataStream<TInstance> dataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}
