using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractTypedDataStream<T> : AbstractDataStream,
                                                         IPendingDataStream
        where T : unmanaged
    {
        //Deliberately NOT getters because that messes up what the Safety Handle points to. 
        public UnsafeTypedStream<T> Pending;

        public unsafe void* PendingWriterPointer { get; }

        protected unsafe AbstractTypedDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Pending = new UnsafeTypedStream<T>(Allocator.Persistent);
            PendingWriterPointer = Pending.AsWriter().GetBufferPointer();
        }

        protected override void DisposeDataStream()
        {
            Pending.Dispose();
            base.DisposeDataStream();
        }
    }
}
