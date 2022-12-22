using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsDataSource : AbstractDataSource<EntityProxyInstanceID>
    {
        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            return dependsOn;
        }
    }
}
