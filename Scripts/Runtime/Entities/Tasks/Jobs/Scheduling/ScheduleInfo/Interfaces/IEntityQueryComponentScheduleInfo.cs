using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IEntityQueryComponentScheduleInfo<T> : IScheduleInfo
        where T : struct, IComponentData
    {
        
    }
}
