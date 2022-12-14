using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DynamicBufferAccessWrapper<T> : AbstractAccessWrapper
        where T : struct, IBufferElementData
    {
        private readonly SystemBase m_System;

        public DynamicBufferAccessWrapper(AccessType accessType, AbstractJobConfig.Usage usage, SystemBase system) : base(accessType, usage)
        {
            m_System = system;
        }

        public DBFEForRead<T> CreateDynamicBufferReader()
        {
            return new DBFEForRead<T>(m_System);
        }

        public DBFEForExclusiveWrite<T> CreateDynamicBufferExclusiveWriter()
        {
            return new DBFEForExclusiveWrite<T>(m_System);
        }

        public override JobHandle Acquire()
        {
            //Do nothing, Unity's System will handle dependencies for us
            return default;
        }

        public override void Release(JobHandle dependsOn)
        {
            //Do nothing - Unity's System will handle dependencies for us
        }
    }
}
