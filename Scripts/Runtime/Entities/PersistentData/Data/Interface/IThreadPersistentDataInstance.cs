namespace Anvil.Unity.DOTS.Entities
{
    public interface IThreadPersistentDataInstance
    {
        public void ConstructForThread(int threadIndex);
        public void DisposeForThread(int threadIndex);
    }
}
