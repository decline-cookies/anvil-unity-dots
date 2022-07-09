using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    internal delegate JobHandle VirtualDataBulkScheduleDelegate(AbstractVirtualData element, JobHandle dependsOn, CancelData cancelData);
}
