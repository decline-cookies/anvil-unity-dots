using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct JobInstanceReader<TInstance>
        where TInstance : struct
    {
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;
        //TODO: Add lookup?
        
        public int Length
        {
            get => m_Iteration.Length;
        }

        public JobInstanceReader(NativeArray<TInstance> iteration)
        {
            m_Iteration = iteration;
        }

        public TInstance this[int index]
        {
            get
            {
                return m_Iteration[index];
            }
        }
    }
}
