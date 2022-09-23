using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    public class UpdateJobScheduleInfo<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public int BatchSize
        {
            get;
        }
    }
}
