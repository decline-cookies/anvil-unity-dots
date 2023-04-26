using Anvil.Unity.DOTS.Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Sets a value on the component data for all entities that match a query.
    /// The query must include write access to the component being set.
    /// </summary>
    /// <typeparam name="T">The <see cref="IComponentData"/> type.</typeparam>
    [BurstCompile]
    public struct FloodSetComponentJob<T> : IJobChunk where T : struct, IComponentData
    {
        private ComponentTypeHandle<T> m_TypeHandle;
        private readonly T m_Value;

        /// <summary>
        /// Creates an instance of the job.
        /// </summary>
        /// <param name="typeHandle">The type handle for the component. Must have write access.</param>
        /// <param name="value">The value to set on all of the entities.</param>
        public FloodSetComponentJob(ComponentTypeHandle<T> typeHandle, T value)
        {
            Debug.Assert(!typeHandle.IsReadOnly);

            m_TypeHandle = typeHandle;
            m_Value = value;
        }

        public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            UnsafeCollectionUtil.FloodSetBuffer(chunk.GetComponentDataPtrRW(ref m_TypeHandle), 0, chunk.Count, m_Value);
        }
    }
}