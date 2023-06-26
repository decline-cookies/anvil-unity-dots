namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DataStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityKeyedTask
    {
        public DataStreamJobConfig(ITaskSetOwner taskSetOwner, EntityProxyDataStream<TInstance> dataStream)
            : base(taskSetOwner)
        {
            RequireDataStreamForRead(dataStream);
        }
    }
}