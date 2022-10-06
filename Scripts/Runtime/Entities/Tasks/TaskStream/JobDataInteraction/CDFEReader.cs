using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct CDFEReader<T>
        where T : struct, IComponentData
    {
        [ReadOnly] private readonly ComponentDataFromEntity<T> m_CDFE;

        internal CDFEReader(SystemBase system)
        {
            m_CDFE = system.GetComponentDataFromEntity<T>(true);
        }

        public T this[Entity entity]
        {
            get => m_CDFE[entity];
        }
    }
}
