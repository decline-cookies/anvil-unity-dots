using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Similar to <see cref="ScheduleGranularity"/> but can be used outside of Entity specific jobs.
    /// Allows for choosing how to batch work to be done in jobs for the two most common cases.
    /// </summary>
    public enum BatchStrategy
    {
        /// <summary>
        /// Calculates the batch size to be however many elements can fit into the Chunk size of 16kb.
        /// Use this for work that is relatively quick to perform.
        /// </summary>
        MaximizeChunk,

        /// <summary>
        /// Spreads the work out across as many threads are available so that the total amount of work is
        /// as balanced as possible.
        /// Use this for work that is intensive and takes a long time. 
        /// </summary>
        MaximizeThreads
    }
}
