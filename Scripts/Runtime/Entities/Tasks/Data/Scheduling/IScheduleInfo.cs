using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IScheduleInfo
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get;
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }
    }
}
