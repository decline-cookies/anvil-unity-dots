namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents an instance of data that is keyed off an associated thread index for use
    /// in <see cref="IThreadPersistentData{T}"/>
    /// </summary>
    public interface IThreadPersistentDataInstance
    {
        /// <summary>
        /// Called for each thread to create the data that should be associated to the thread index.
        /// </summary>
        /// <param name="threadIndex">The thread index to be associated with</param>
        public void ConstructForThread(int threadIndex);
        
        /// <summary>
        /// Called whenever an element is being disposed during cleanup.
        /// This allows for custom cleanup code.
        /// </summary>
        /// <param name="threadIndex">The thread index that was associated with this data</param>
        public void DisposeForThread(int threadIndex);
    }
}
