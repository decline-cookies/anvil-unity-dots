namespace Anvil.Unity.DOTS.Entities
{
    public interface ITaskStream<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public ProxyDataStream<TInstance> DataStream
        {
            get;
        }
    }
}
