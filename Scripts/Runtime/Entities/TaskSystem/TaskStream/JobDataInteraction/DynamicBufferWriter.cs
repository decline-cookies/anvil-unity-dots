using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct DynamicBufferWriter<T>
        where T : struct, IBufferElementData
    {
        [WriteOnly] private BufferFromEntity<T> m_BFE;

        internal DynamicBufferWriter(SystemBase system)
        {
            m_BFE = system.GetBufferFromEntity<T>(false);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_BFE[entity];
        }
    }
}
