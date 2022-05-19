using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Extension methods for <see cref="SystemBase"/> for use with <see cref="DynamicBufferSharedWriteHandle"/>
    /// </summary>
    public static class DynamicBufferSharedWriteHandleSystemExtension
    {
        /// <summary>
        /// Gets a <see cref="DynamicBufferSharedWriteHandle"/> for a given <see cref="IBufferElementData"/>
        /// </summary>
        /// <param name="sharedWriteSystem">
        /// The <see cref="SystemBase"/> that wishes to perform a shared write
        /// on a <see cref="DynamicBuffer{T}"/>
        /// </param>
        /// <typeparam name="T">
        /// he type of <see cref="IBufferElementData"/> in the <see cref="DynamicBuffer{T}"/>
        /// </typeparam>
        /// <returns>An instance of <see cref="DynamicBufferSharedWriteHandle"/></returns>
        public static DynamicBufferSharedWriteHandle GetDynamicBufferSharedWriteHandle<T>(this SystemBase sharedWriteSystem)
            where T : IBufferElementData
        {
            DynamicBufferSharedWriteDataSystem dataSystem = sharedWriteSystem.World.GetOrCreateSystem<DynamicBufferSharedWriteDataSystem>();
            DynamicBufferSharedWriteController<T> controller = dataSystem.GetOrCreate<T>();
            return new DynamicBufferSharedWriteHandle(controller, sharedWriteSystem);
        }
    }
}
