using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IDeferredScheduleInfo : IScheduleInfoBase
    {
        /// <summary>
        /// Specific pointer information about a <see cref="DeferredNativeArray{T}"/> used for scheduling
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }
    }
}
