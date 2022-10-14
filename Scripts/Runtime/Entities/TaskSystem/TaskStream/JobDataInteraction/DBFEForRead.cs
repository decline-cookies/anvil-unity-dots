using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public readonly struct DBFEForRead<T>
        where T : struct, IBufferElementData
    {
        [ReadOnly] private readonly BufferFromEntity<T> m_BFE;

        internal DBFEForRead(SystemBase system)
        {
            m_BFE = system.GetBufferFromEntity<T>(true);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_BFE[entity];
        }
    }
}
