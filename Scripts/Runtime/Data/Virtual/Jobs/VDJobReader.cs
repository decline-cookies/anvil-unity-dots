using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct VDJobReader<TInstance>
        where TInstance : struct
    {
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;

        public int Length
        {
            get => m_Iteration.Length;
        }

        public VDJobReader(NativeArray<TInstance> iteration)
        {
            m_Iteration = iteration;
        }

        public TInstance this[int index]
        {
            get => m_Iteration[index];
        }
    }
}
