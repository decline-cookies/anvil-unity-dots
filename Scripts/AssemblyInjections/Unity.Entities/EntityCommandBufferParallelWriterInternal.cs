using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class to access the internals of a <see cref="EntityCommandBuffer.ParallelWriter"/>
    /// </summary>
    public static class EntityCommandBufferParallelWriterInternal
    {
        /// <summary>
        /// Sets the Thread Index on a <see cref="EntityCommandBuffer.ParallelWriter"/> so that commands are written
        /// to the correct Thread Chain
        /// </summary>
        /// <remarks>
        /// This is not normally required and should be rarely used.
        /// A <see cref="EntityCommandBuffer.ParallelWriter"/> gets it's thread index only once it enters into the
        /// executing job that it will be used in. When accessing this writer struct from within a collection, it will
        /// not have gotten it's thread index assigned so this function serves as a way to inject it. 
        /// </remarks>
        /// <param name="ecbParallelWriter">The <see cref="EntityCommandBuffer.ParallelWriter"/></param>
        /// <param name="threadIndex">The thread index to set</param>
        public static void SetThreadIndex(ref this EntityCommandBuffer.ParallelWriter ecbParallelWriter, int threadIndex)
        {
            ecbParallelWriter.m_ThreadIndex = threadIndex;
        }
    }
}
