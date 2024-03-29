using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class to access the internals of an <see cref="EntityCommandBuffer"/>
    /// </summary>
    public static class EntityCommandBufferInternal
    {
        /// <summary>
        /// Returns whether the <see cref="EntityCommandBuffer"/> has been played back or not.
        /// </summary>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to check.</param>
        /// <returns>
        /// true if it has not been disposed and has been played back, false otherwise.
        /// </returns>
        public static unsafe bool DidPlayback(this EntityCommandBuffer ecb)
        {
            return ecb.IsCreated && ecb.m_Data->m_DidPlayback;
        }
    }
}
