using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct DataResponseJobStruct<T>
        where T : struct, ICompleteData<T>
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
        private UnsafeTypedStream<T>.Writer m_CompleteWriter;
    }
}
