namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ITaskStreamScheduleInfo<TInstance> : IDeferredScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {
        
    }
}
