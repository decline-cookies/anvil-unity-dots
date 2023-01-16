using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class GenericDataAccessWrapper<TData> : AbstractAccessWrapper
        where TData : struct
    {
        private readonly AccessControlledValue<TData> m_AccessControlledData;
        private TData m_Data;

        public TData Data
        {
            get => m_Data;
        }

        public GenericDataAccessWrapper(AccessControlledValue<TData> data, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            m_AccessControlledData = data;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_AccessControlledData.AcquireAsync(AccessType, out m_Data);
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessControlledData.ReleaseAsync(dependsOn);
        }
    }
}
