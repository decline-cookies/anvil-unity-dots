using Anvil.Unity.DOTS.Entities.TaskDriver;

namespace Anvil.Unity.DOTS.Entities
{
    internal class PersistentData<T> : AbstractTypedPersistentData<T>, IPersistentData<T> where T : unmanaged
    {
        public PersistentData(uint id, T data) : base(id, data) { }

        public PersistentDataReader<T> CreatePersistentDataReader()
        {
            return new PersistentDataReader<T>(ref Data);
        }

        public PersistentDataWriter<T> CreatePersistentDataWriter()
        {
            return new PersistentDataWriter<T>(ref Data);
        }
    }
}