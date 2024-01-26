using Unity.Collections;

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    public interface ISWTCBuffer
    {
        public NativeArray<int> Buffer { get; }
    }
#endif
}