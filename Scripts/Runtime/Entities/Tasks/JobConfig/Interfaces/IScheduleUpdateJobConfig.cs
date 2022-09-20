namespace Anvil.Unity.DOTS.Entities
{
    public interface IScheduleUpdateJobConfig
    {
        public IUpdateJobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance;
    }
}
