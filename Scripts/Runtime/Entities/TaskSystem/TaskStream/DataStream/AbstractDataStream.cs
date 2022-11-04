using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractDataStream<TInstance> : AbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //Deliberately NOT getters because that messes up what the Safety Handle points to. 
        //TODO: Elaborate
        internal UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> Pending;
        internal DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> Live;

        internal sealed override unsafe void* PendingWriterPointer
        {
            get;
        }

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => Live.ScheduleInfo;
        }
        
        protected unsafe AbstractDataStream(bool isCancellable, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(isCancellable, taskDriver, taskSystem)
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
        
        protected AbstractDataStream(bool isCancellable, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            IsCancellable = isCancellable;
        }
        
        //TODO: Probably a nicer way to handle this without abstract
        internal abstract AbstractDataStream GetCancelPendingDataStream();
    }
}
