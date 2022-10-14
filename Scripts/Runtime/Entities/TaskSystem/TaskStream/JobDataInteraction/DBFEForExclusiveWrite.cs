using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct DBFEForExclusiveWrite<T>
        where T : struct, IBufferElementData
    {
        [NativeDisableParallelForRestriction][WriteOnly] private BufferFromEntity<T> m_BFE;

        internal DBFEForExclusiveWrite(SystemBase system)
        {
            m_BFE = system.GetBufferFromEntity<T>(false);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_BFE[entity];
        }
    }
}
