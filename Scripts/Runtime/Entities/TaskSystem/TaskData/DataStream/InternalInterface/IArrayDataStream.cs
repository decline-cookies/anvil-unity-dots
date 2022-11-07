using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IArrayDataStream
    {
        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }
    }
}
