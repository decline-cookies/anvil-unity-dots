using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public readonly struct DynamicBufferReader<T>
        where T : struct, IBufferElementData
    {
        [ReadOnly] private readonly BufferFromEntity<T> m_BFE;

        internal DynamicBufferReader(SystemBase system)
        {
            m_BFE = system.GetBufferFromEntity<T>(true);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_BFE[entity];
        }
    }
}
