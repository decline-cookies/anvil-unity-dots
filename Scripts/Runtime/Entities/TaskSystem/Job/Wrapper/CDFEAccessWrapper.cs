using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CDFEAccessWrapper<T> : AbstractAccessWrapper
        where T : struct, IComponentData
    {
        private readonly SystemBase m_System;
        
        internal class CDFEType
        {
        }

        public CDFEAccessWrapper(AccessType accessType, SystemBase system) : base(accessType)
        {
            m_System = system;
        }

        public CDFEReader<T> CreateCDFEReader()
        {
            return new CDFEReader<T>(m_System);
        }

        public CDFEWriter<T> CreateCDFEUpdater()
        {
            return new CDFEWriter<T>(m_System);
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
