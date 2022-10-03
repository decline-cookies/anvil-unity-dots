using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ITaskSystem
    {
        public byte Context { get; }

        public World World { get; }
        
        //TODO: Maybe just make an AbstractTaskSystem without generics for this
        internal CancelRequestsDataStream GetCancelRequestsDataStream();
    }
}
