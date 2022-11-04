using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelFlowAccessWrapper : AbstractAccessWrapper
    {
        //TODO: This is gross but it's because the TaskDriver may not yet have its CancelFlow created when the job is 
        //TODO: scheduled. But by the time the job executes it will be.
        public TaskDriverCancelFlow CancelFlow
        {
            get => m_TaskDriver.CancelFlow;
        }

        private readonly AbstractTaskDriver m_TaskDriver;

        public CancelFlowAccessWrapper(AbstractTaskDriver taskDriver, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            m_TaskDriver = taskDriver;
        }

        public sealed override JobHandle Acquire()
        {
            return CancelFlow.AcquireAsync(AccessType);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            CancelFlow.ReleaseAsync(releaseAccessDependency);
        }
    }
}
