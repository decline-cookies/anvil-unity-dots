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
        /// <param name="ecbParallelWriter">The <see cref="EntityCommandBuffer.ParallelWriter"/></param>
        /// <param name="threadIndex">The thread index to set</param>
        public static void SetThreadIndex(ref this EntityCommandBuffer.ParallelWriter ecbParallelWriter, int threadIndex)
        {
            ecbParallelWriter.m_ThreadIndex = threadIndex;
        }
    }
}
