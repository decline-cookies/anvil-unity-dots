using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct UniqueIDProvider
    {
        private UnsafeParallelHashMap<int, uint> m_IDsPerThread;
        
        
    }
}
