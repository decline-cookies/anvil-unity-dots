namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ICancelTaskStreamScheduleInfo<TInstance> : IDeferredScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {
        
    }
}
