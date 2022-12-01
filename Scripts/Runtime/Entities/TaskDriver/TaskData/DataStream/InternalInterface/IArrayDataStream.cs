using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IArrayDataStream : IPendingDataStream
    {
        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }
    }
}
