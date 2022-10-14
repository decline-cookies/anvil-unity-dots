using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DynamicBufferAccessWrapper<T> : AbstractAccessWrapper
        where T : struct, IBufferElementData
    {
        private readonly SystemBase m_System;
        private readonly DynamicBufferSharedWriteHandle m_DynamicBufferSharedWriteHandle;

        internal class DynamicBufferType
        {
        }

        public DynamicBufferAccessWrapper(AccessType accessType, SystemBase system) : base(accessType)
        {
            m_System = system;
            m_DynamicBufferSharedWriteHandle = m_System.GetDynamicBufferSharedWriteHandle<T>();
        }

        public DynamicBufferReader<T> CreateDynamicBufferReader()
        {
            return new DynamicBufferReader<T>(m_System);
        }

        public DynamicBufferWriter<T> CreateDynamicBufferWriter()
        {
            return new DynamicBufferWriter<T>(m_System);
        }

        public DynamicBufferSharedWriter<T> CreateDynamicBufferSharedWriter()
        {
            return new DynamicBufferSharedWriter<T>(m_System);
        }

        public override JobHandle Acquire()
        {
            //Do nothing, Unity's System will handle dependencies for us
            return default;
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            //Do nothing - Unity's System will handle dependencies for us
        }
    }
}
