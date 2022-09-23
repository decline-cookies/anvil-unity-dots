using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities.DataStream
{
    public class TaskDataStream<TInstance> : AbstractAnvilBase
        where TInstance : unmanaged, IProxyInstance
    {
        internal readonly ProxyDataStream<TInstance> DataStream;
        internal readonly ProxyDataStream<TInstance> PendingCancelDataStream;

        public TaskDataStream()
        {
            DataStream = new ProxyDataStream<TInstance>();
            PendingCancelDataStream = new ProxyDataStream<TInstance>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            PendingCancelDataStream.Dispose();
            
            base.DisposeSelf();
        }
    }
}
