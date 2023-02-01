using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DynamicBufferAccessWrapper<T> : AbstractAccessWrapper
        where T : struct, IBufferElementData
    {
        private readonly SystemBase m_System;
        private readonly AccessController m_AccessController;

        public DynamicBufferAccessWrapper(AccessType accessType, AbstractJobConfig.Usage usage, SystemBase system) : base(accessType, usage)
        {
            m_System = system;
            m_AccessController = m_System.World.GetOrCreateSystem<TaskDriverManagementSystem>().GetOrCreateDBFEAccessController<T>();
        }

        protected override void DisposeSelf()
        {
            //NOT disposing the AccessController because it is owned by the TaskDriverManagementSystem and shared across
            //multiple AccessWrappers.
            base.DisposeSelf();
        }

        public DBFEForRead<T> CreateDynamicBufferReader()
        {
            return new DBFEForRead<T>(m_System);
        }

        public DBFEForExclusiveWrite<T> CreateDynamicBufferExclusiveWriter()
        {
            return new DBFEForExclusiveWrite<T>(m_System);
        }

        public override JobHandle AcquireAsync()
        {
            return m_AccessController.AcquireAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessController.ReleaseAsync(dependsOn);
        }
    }
}
