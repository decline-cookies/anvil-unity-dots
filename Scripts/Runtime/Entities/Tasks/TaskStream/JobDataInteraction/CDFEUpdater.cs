using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct CDFEUpdater<T>
        where T : struct, IComponentData
    {
        [NativeDisableParallelForRestriction] private ComponentDataFromEntity<T> m_CDFE;

        internal CDFEUpdater(SystemBase system)
        {
            m_CDFE = system.GetComponentDataFromEntity<T>(false);
        }

        public T this[Entity entity]
        {
            get => m_CDFE[entity];
            set => m_CDFE[entity] = value;
        }
    }
}
