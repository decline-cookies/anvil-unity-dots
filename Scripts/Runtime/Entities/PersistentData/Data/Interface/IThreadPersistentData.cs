namespace Anvil.Unity.DOTS.Entities
{
    public interface IThreadPersistentData<T> : IAbstractPersistentData
        where T : struct
    {
        public delegate T ConstructionCallbackPerThread(int threadIndex);

        public delegate void DisposalCallbackPerThread(int threadIndex, T threadData);
    }
}
