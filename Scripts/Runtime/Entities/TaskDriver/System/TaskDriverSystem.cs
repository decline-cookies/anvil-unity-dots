using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #188 - /// Document public API
    public partial class TaskDriverSystem<TTaskDriverType> : AbstractTaskDriverSystem
        where TTaskDriverType : AbstractTaskDriver
    {
        public TaskDriverSystem(World world) : base(world) { }
    }
}