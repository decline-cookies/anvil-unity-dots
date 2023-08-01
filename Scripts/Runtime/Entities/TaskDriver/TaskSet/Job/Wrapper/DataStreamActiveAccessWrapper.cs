using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: Can we simplify this a lot and get rid of a bunch of special Access Wrapper types?
    //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/105#discussion_r1043593841
    internal class DataStreamActiveAccessWrapper<T> : AbstractDataStreamActiveAccessWrapper<T>
        where T : unmanaged, IEntityKeyedTask
    {
        public DataStreamActiveAccessWrapper(
            EntityProxyDataStream<T> dataStream,
            AccessType accessType,
            AbstractJobConfig.Usage usage) :
            base(dataStream, accessType, usage) { }

        public override JobHandle AcquireAsync()
        {
            return DataStream.AcquireActiveAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            DataStream.ReleaseActiveAsync(dependsOn);
        }
    }
}
