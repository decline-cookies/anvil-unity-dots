namespace Anvil.Unity.DOTS.Entities.Tasks
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CancellableTaskStream<TInstance> : TaskStream<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public readonly ProxyDataStream<TInstance> PendingCancelDataStream;

        internal sealed override bool IsCancellable
        {
            get => true;
        }

        public CancellableTaskStream()
        {
            PendingCancelDataStream = new ProxyDataStream<TInstance>();
        }

        internal sealed override AbstractProxyDataStream GetPendingCancelDataStream()
        {
            return PendingCancelDataStream;
        }
    }
}
