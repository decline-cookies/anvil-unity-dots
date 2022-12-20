// using System;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     internal class ActiveLookupData<T> : AbstractData
//         where T : unmanaged, IEquatable<T>
//     {
//         private UnsafeParallelHashMap<T, bool> m_Lookup;
//
//         public ActiveLookupData(uint id) : base(id)
//         {
//             m_Lookup = new UnsafeParallelHashMap<T, bool>(ChunkUtil.MaxElementsPerChunk<T>(), Allocator.Persistent);
//         }
//
//         protected sealed override void DisposeData()
//         {
//             m_Lookup.Dispose();
//         }
//     }
// }
