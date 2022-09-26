using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface ITaskSystem
    {
        public byte Context { get; }

        public World World { get; }
    }
}
