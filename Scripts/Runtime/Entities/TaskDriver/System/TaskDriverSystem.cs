using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal partial class TaskDriverSystem<TTaskDriverType> : AbstractTaskDriverSystem
        where TTaskDriverType : AbstractTaskDriver
    {
        public TaskDriverSystem(World world) : base(world)
        {
        }
    }
}
