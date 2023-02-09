using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataSource : AbstractDataSource<EntityProxyInstanceID>
    {
        public CancelProgressDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem) { }

        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            throw new InvalidOperationException($"CancelProgress Data Never needs to be consolidated");
        }
    }
}
