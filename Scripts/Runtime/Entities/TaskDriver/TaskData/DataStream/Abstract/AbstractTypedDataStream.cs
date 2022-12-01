using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
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
        
#if DEBUG
        protected internal sealed override unsafe long Debug_PendingBytesPerInstance
        {
            get => sizeof(T);
        }

        protected internal sealed override Type Debug_InstanceType
        {
            get => typeof(T);
        }
#endif

        protected unsafe AbstractTypedDataStream(AbstractWorkload owningWorkload) : base(owningWorkload)
        {
            Pending = new UnsafeTypedStream<T>(ChunkUtil.MaxElementsPerChunk<T>() / 8, Allocator.Persistent, Allocator.Persistent, ParallelAccessUtil.CollectionSizeForMaxThreads);
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
