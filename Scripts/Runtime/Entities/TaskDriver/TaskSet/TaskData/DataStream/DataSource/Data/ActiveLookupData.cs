using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ActiveLookupData<T> : AbstractData
        where T : unmanaged, IEquatable<T>
    {
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<T>() / 8.0f);
        
        private UnsafeParallelHashMap<T, bool> m_Lookup;

        public ActiveLookupData(uint id) : base(id)
        {
            m_Lookup = new UnsafeParallelHashMap<T, bool>(INITIAL_SIZE, Allocator.Persistent);
        }

        protected sealed override void DisposeData()
        {
            m_Lookup.Dispose();
        }
    }
}
