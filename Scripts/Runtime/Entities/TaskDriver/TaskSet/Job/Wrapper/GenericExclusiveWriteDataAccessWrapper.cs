using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class GenericExclusiveWriteDataAccessWrapper<TData> : AbstractAccessWrapper
        where TData : struct
    {
        private readonly IExclusiveWriteAccessControlledValue<TData> m_AccessControlledData;
        private TData m_Data;

        public TData Data
        {
            get => m_Data;
        }

        public GenericExclusiveWriteDataAccessWrapper(IExclusiveWriteAccessControlledValue<TData> data, AbstractJobConfig.Usage usage) : base(AccessType.ExclusiveWrite, usage)
        {
            m_AccessControlledData = data;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_AccessControlledData.AcquireExclusiveWriteAsync(out m_Data);
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessControlledData.ReleaseAsync(dependsOn);
        }
    }
}