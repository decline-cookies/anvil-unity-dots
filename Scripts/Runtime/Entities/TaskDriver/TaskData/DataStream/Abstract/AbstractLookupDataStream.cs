using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractLookupDataStream<T> : AbstractTypedDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        public UnsafeParallelHashMap<T, bool> Lookup;
        
#if DEBUG
        protected internal sealed override unsafe long Debug_LiveBytesPerInstance
        {
            get => sizeof(T) + sizeof(bool);
        }
#endif

        protected AbstractLookupDataStream(AbstractTaskSet owningTaskSet) : base(owningTaskSet)
        {
            Lookup = new UnsafeParallelHashMap<T, bool>(ChunkUtil.MaxElementsPerChunk<T>() / 8, Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            Lookup.Dispose();
            base.DisposeDataStream();
        }
        
        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
    }
}
