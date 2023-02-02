namespace Anvil.Unity.DOTS.Entities.TaskDriver
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
        
        protected DataStreamJobConfig(ITaskSetOwner taskSetOwner)
            : base(taskSetOwner)
        {
        }
    }
}
