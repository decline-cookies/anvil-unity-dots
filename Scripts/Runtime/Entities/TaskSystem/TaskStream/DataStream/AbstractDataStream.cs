using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractDataStream<TInstance> : AbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> Pending { get; }
        internal DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> Live { get; }

        internal sealed override unsafe void* PendingWriterPointer
        {
            get;
        }

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => Live.ScheduleInfo;
        }
        
        protected unsafe AbstractDataStream(bool isCancellable) : base(isCancellable)
        {
            Pending = new UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            Live = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent,
                                                                                  Allocator.TempJob);

            PendingWriterPointer = Pending.AsWriter().GetBufferPointer();
        }

        protected override void DisposeDataStream()
        {
            Pending.Dispose();
            Live.Dispose();
            base.DisposeDataStream();
        }
    }

    public abstract class AbstractDataStream : AbstractConsolidatableDataStream
    {
        internal bool IsCancellable { get; }
        
        //TODO: Probably a nicer way to handle this without abstract
        internal abstract unsafe void* PendingWriterPointer { get; }
        
        
        protected AbstractDataStream(bool isCancellable)
        {
            IsCancellable = isCancellable;
        }
        
        //TODO: Probably a nicer way to handle this without abstract
        internal abstract AbstractDataStream GetCancelPendingDataStream();
    }
}
