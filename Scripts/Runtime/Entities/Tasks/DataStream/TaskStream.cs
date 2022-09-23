namespace Anvil.Unity.DOTS.Entities
{
    public class TaskStream<TInstance> : ITaskStream<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public ProxyDataStream<TInstance> DataStream
        {
            get;
        }

        public TaskStream()
        {
            DataStream = new ProxyDataStream<TInstance>();
        }
    }
}
