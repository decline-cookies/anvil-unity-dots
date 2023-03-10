using System;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents an instance of data that is keyed off an associated thread index for use
    /// in <see cref="IThreadPersistentData{T}"/>
    /// </summary>
    public interface IThreadPersistentDataInstance : IDisposable
    {
        /// <summary>
        /// Called for each thread to create the data that should be associated to the thread index.
        /// </summary>
        /// <param name="threadIndex">The thread index to be associated with</param>
        public void ConstructForThread(int threadIndex);
    }
}
