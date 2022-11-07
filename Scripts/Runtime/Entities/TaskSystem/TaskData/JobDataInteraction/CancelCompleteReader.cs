using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public readonly struct CancelCompleteReader
    {
        [ReadOnly] private readonly NativeArray<EntityProxyInstanceID> m_Iteration;

        internal CancelCompleteReader(NativeArray<EntityProxyInstanceID> iteration)
        {
            m_Iteration = iteration;
        }

        public Entity this[int index]
        {
            get => m_Iteration[index].Entity;
        }
    }
}
