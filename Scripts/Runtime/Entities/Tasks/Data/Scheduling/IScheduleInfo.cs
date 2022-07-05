using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Common interface for job scheduling parameters.
    /// </summary>
    public interface IScheduleInfo
    {
        /// <summary>
        /// The number of elements in a batch
        /// </summary>
        public int BatchSize
        {
            get;
        }
        
        /// <summary>
        /// The total length of all elements
        /// </summary>
        public int Length
        {
            get;
        }
        
        /// <summary>
        /// Specific pointer information about a <see cref="DeferredNativeArray{T}"/> used for scheduling
        /// </summary>
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }
    }
}
