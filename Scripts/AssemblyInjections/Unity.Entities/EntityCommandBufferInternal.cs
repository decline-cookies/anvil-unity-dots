using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class to access the internals in an <see cref="EntityCommandBuffer"/>
    /// </summary>
    public static class EntityCommandBufferInternal
    {
        /// <summary>
        /// Returns whether the <see cref="EntityCommandBuffer"/> has been played back or not.
        /// </summary>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to check.</param>
        /// <returns>true if it has been played back, false if not</returns>
        public static unsafe bool DidPlayback(this EntityCommandBuffer ecb)
        {
            return ecb.m_Data == null || ecb.m_Data->m_DidPlayback;
        }
    }
}
