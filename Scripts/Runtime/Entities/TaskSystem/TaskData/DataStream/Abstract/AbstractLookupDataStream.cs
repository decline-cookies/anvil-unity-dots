using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractLookupDataStream<T> : AbstractTypedDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        public UnsafeParallelHashMap<T, bool> Lookup;

        public AbstractLookupDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            Lookup = new UnsafeParallelHashMap<T, bool>(ChunkUtil.MaxElementsPerChunk<T>(), Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            Lookup.Dispose();
            base.DisposeDataStream();
        }
    }
}
