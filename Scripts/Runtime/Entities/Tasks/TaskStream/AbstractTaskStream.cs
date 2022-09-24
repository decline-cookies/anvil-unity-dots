namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskStream
    {
        internal abstract bool IsCancellable { get; }
        internal abstract AbstractProxyDataStream GetDataStream();
        internal abstract AbstractProxyDataStream GetPendingCancelDataStream();
    }
}
