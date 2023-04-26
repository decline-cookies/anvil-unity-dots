using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class GenericSharedWriteDataAccessWrapper<TData> : AbstractAccessWrapper
        where TData : struct
    {
        private readonly ISharedWriteAccessControlledValue<TData> m_AccessControlledData;
        private TData m_Data;

        public TData Data
        {
            get => m_Data;
        }

        public GenericSharedWriteDataAccessWrapper(ISharedWriteAccessControlledValue<TData> data, AbstractJobConfig.Usage usage) : base(AccessType.SharedWrite, usage)
        {
            m_AccessControlledData = data;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_AccessControlledData.AcquireSharedWriteAsync(out m_Data);
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessControlledData.ReleaseAsync(dependsOn);
        }
    }
}