using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    public readonly struct ResponseJobReader<TResponse>
        where TResponse : struct
    {
        private readonly NativeArray<TResponse> m_Current;

        public int Length
        {
            get => m_Current.Length;
        }
        
        public ResponseJobReader(NativeArray<TResponse> current)
        {
            m_Current = current;
        }

        public TResponse this[int index]
        {
            get => m_Current[index];
        }
    }
}
