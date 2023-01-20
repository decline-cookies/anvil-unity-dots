using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class PersistentDataAccessWrapper<TPersistentData> : AbstractAccessWrapper
        where TPersistentData : AbstractPersistentData
    {

        public TPersistentData PersistentData { get; }
        
        public PersistentDataAccessWrapper(TPersistentData persistentData, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            PersistentData = persistentData;
        }

        public override JobHandle AcquireAsync()
        {
            return PersistentData.AcquireAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            PersistentData.ReleaseAsync(dependsOn);
        }
    }
}
