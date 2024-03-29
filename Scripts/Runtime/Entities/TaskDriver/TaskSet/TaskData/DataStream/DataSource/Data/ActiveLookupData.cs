using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class ActiveLookupData<T> : AbstractData
        where T : unmanaged, IEquatable<T>
    {
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<T>() / 8.0f);

        public UnsafeParallelHashMap<T, bool> Lookup { get; }

        public ActiveLookupData(
            IDataOwner dataOwner, 
            CancelRequestBehaviour cancelRequestBehaviour, 
            string uniqueContextIdentifier) 
            : base(
                dataOwner, 
                cancelRequestBehaviour, 
                null,
                uniqueContextIdentifier)
        {
            Lookup = new UnsafeParallelHashMap<T, bool>(INITIAL_SIZE, Allocator.Persistent);
        }

        protected sealed override void DisposeData()
        {
            Lookup.Dispose();
        }
    }
}
