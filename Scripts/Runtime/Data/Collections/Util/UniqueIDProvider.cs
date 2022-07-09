using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct UniqueIDProvider
    {
        private uint m_LastID;
        
        private UnsafeParallelHashMap<int, uint> m_IDsPerThread;
        
        //4,294,967,295 possible values
        //Divide by number of threads (ex 16) = 268,435,455
        
    }
}
