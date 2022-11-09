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

        //Used by ResolveTargets as a way to store a reference to the typed Pending stream in an untyped and Burst
        //compatible way. We'll interpret back to the typed Writer when resolving.
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
        
        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
    }
}
