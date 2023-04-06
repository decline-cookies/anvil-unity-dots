using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class GenericDataReadOnlyAccessWrapper<TData> : AbstractAccessWrapper
        where TData : struct
    {
        private readonly IReadOnlyAccessControlledValue<TData> m_AccessControlledData;
        private TData m_Data;

        public TData Data
        {
            get => m_Data;
        }

        public GenericDataReadOnlyAccessWrapper(IReadOnlyAccessControlledValue<TData> data, AbstractJobConfig.Usage usage) : base(AccessType.SharedRead, usage)
        {
            m_AccessControlledData = data;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_AccessControlledData.AcquireReadOnlyAsync(out m_Data);
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessControlledData.ReleaseAsync(dependsOn);
        }
    }
}
