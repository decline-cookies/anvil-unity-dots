using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal partial class TaskDriverSystem<TTaskDriverType> : AbstractTaskDriverSystem
        where TTaskDriverType : AbstractTaskDriver
    {
        public TaskDriverSystem(World world) : base(world)
        {
        }
    }
}
