using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    public readonly struct UpdateResponseJobData<TResponse>
        where TResponse : struct
    {
        private readonly NativeArray<TResponse> m_Current;

        public UpdateResponseJobData(NativeArray<TResponse> current)
        {
            m_Current = current;
        }

        public TResponse this[int index]
        {
            get => m_Current[index];
        }
    }
}
