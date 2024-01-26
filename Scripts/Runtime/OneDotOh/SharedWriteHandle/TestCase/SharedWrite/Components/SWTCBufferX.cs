using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    public struct SWTCBufferX : IComponentData,
                                ISWTCBuffer
    {
        public NativeArray<int> Buffer
        {
            get;
        }

        public SWTCBufferX(NativeArray<int> buffer)
        {
            Buffer = buffer;
        }
    }
#endif
}