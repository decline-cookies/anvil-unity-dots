using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class for dealing with optimal numbers of elements in a chunk
    /// </summary>
    public static class ChunkUtil
    {
        //See Chunk.kChunkSize - Can't access so we redefine here
        private const int CHUNK_SIZE = 16 * 1024;
        
        /// <summary>
        /// Gets the maximum number of elements that will fit into a chunk of memory.
        /// At minimum it will return 1 if the element is larger than a chunk.
        /// </summary>
        /// <typeparam name="T">The type of element</typeparam>
        /// <returns>The maximum number of elements</returns>
        public static int MaxElementsPerChunk<T>()
            where T : struct
        {
            return math.max(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), 1);
        }
    }
}
