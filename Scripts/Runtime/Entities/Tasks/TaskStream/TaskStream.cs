using System;

namespace Anvil.Unity.DOTS.Entities
{
    public class TaskStream<TInstance> : AbstractTaskStream,
                                         ITaskStream<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public ProxyDataStream<TInstance> DataStream { get; }

        internal override bool IsCancellable
        {
            get => false;
        }

        public TaskStream()
        {
            DataStream = new ProxyDataStream<TInstance>();
        }

        internal sealed override AbstractProxyDataStream GetDataStream()
        {
            return DataStream;
        }

        internal override AbstractProxyDataStream GetPendingCancelDataStream()
        {
            throw new NotSupportedException($"Tried to get Pending Cancel Data Stream on {this} but it doesn't exists. Check {IsCancellable} first!");
        }
    }
}
