namespace Anvil.Unity.DOTS.Entities
{
    public class CancellableTaskStream<TInstance> : TaskStream<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public readonly ProxyDataStream<TInstance> PendingCancelDataStream;
        
        public CancellableTaskStream()
        {
            PendingCancelDataStream = new ProxyDataStream<TInstance>();
        }
    }
}
