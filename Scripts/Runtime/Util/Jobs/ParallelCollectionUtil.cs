using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Utility methods for working with Native Collections in a parallel manner.
    /// </summary>
    public static class ParallelCollectionUtil
    {
        /// <remarks>
        /// This is static readonly because Burst will only read from a static variable if it is readonly.
        /// We are unable to set it directly to <see cref="JobsUtility.JobWorkerMaximumCount"/> because that function
        /// is an external function and external functions are not allowed to be called from a static constructor which
        /// is where static readonly variables get assigned.
        /// We are also unable to lazy instantiate or call <see cref="JobsUtility.JobWorkerMaximumCount"/> within the
        /// functions in this class that use it because an external function can only be called from the main thread
        /// and these functions are intended for use inside bursted jobs that run on many different worker threads.
        ///
        /// To avoid this, we have a <see cref="RuntimeInitializeOnLoadMethodAttribute"/> on the
        /// <see cref="ParallelCollectionUtil.Init"/> method which then assigns to a <see cref="SharedStatic{int}"/>
        ///
        /// While a bit hacky, it is ensured to run before any potential jobs execute. The external call is allowed
        /// and we get the correct value and we can assign it to the <see cref="SharedStatic{int}"/>.
        /// Subsequent bursted jobs are then allowed to access it.
        /// <seealso cref="https://docs.unity3d.com/Packages/com.unity.burst@1.7/manual/docs/AdvancedUsages.html#shared-static"/>
        /// </remarks>
        private static readonly SharedStatic<int> JOB_WORKER_MAXIMUM_COUNT = SharedStatic<int>.GetOrCreate<InternalClassJobWorkerMaximumCount>();
        private class InternalClassJobWorkerMaximumCount
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            JOB_WORKER_MAXIMUM_COUNT.Data = JobsUtility.JobWorkerMaximumCount;
            CollectionSizeForMaxThreads = JOB_WORKER_MAXIMUM_COUNT.Data + 1;

            Debug.Assert(JOB_WORKER_MAXIMUM_COUNT.Data > 0);
        }

    
        /// <summary>
        /// Returns an ideal size for the number of buckets a collection should have
        /// to account for all possible threads that could write to it at once.
        /// </summary>
        /// <remarks>
        /// There is a lot of different terminology for the "buckets".
        /// <see cref="NativeStream"/> has foreachCount
        /// <see cref="UnsafeTypedStream"/> has lanes
        /// etc
        /// It's the number of separate "buckets" that can be written to in parallel.
        /// </remarks>
        public static int CollectionSizeForMaxThreads
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the correct index for the collection based on the <paramref name="nativeThreadIndex"/> passed in.
        /// </summary>
        /// <remarks>
        /// This function assumes that the collection being used was sized appropriately via
        /// <see cref="ParallelCollectionUtil.CollectionSizeForMaxThreads"/>. Larger sized collections will still work
        /// but the intended usage is to create a small tightly packed collection sized for the specific hardware the
        /// program is running on. Smaller sized collections will error sporadically as the scheduler assigns jobs to
        /// out of range thread indexes.
        ///
        /// This function also assumes that you are never running your job on the main thread via
        /// <see cref="IJobExtensions.Run"/>. In that case the thread index will be 0 and this function will
        /// return -1 which will cause an error.
        ///
        /// When scheduling your job, Unity will place your job on one of the available worker threads in which case
        /// the native thread index you receive will be from 1 to <see cref="JobsUtility.JobWorkerMaximumCount"/>.
        /// There are two special cases.
        /// 1. The scheduler may place your job on the main thread. In this case the native thread index will be
        /// some value above <see cref="JobsUtility.JobWorkerMaximumCount"/>.
        /// 2. The scheduler may place your job on a profiler thread in the editor. In this case the native thread
        /// index will also be some value above <see cref="JobsUtility.JobWorkerMaximumCount"/>
        /// It is unknown why this is. 
        ///
        /// Our goal is to have a tightly packed collection so in the example of an 8 core machine with
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> equal to 15, we would have the following mapping.
        ///
        /// thread 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, X 
        /// index  0, 1, 2, 3, 4, 5, 6, 7, 8, 9,  10, 11, 12, 13, 14, 15
        ///
        /// We have a tightly packed collection with index 0 through 15 for a total of 16 buckets.
        /// Thread indexes map directly without having to remember the special rules.
        /// </remarks>
        /// <param name="nativeThreadIndex">
        /// The native thread index to figure out the collection index from. Must be greater than 0.
        /// </param>
        /// <returns>The collection index to use</returns>
        public static int CollectionIndexForThread(int nativeThreadIndex)
        {
            DetectMultipleXThreads(nativeThreadIndex);
            Debug.Assert(nativeThreadIndex > 0 && nativeThreadIndex <= JobsUtility.MaxJobThreadCount);
            return math.min(nativeThreadIndex - 1, JOB_WORKER_MAXIMUM_COUNT.Data);
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private static void DetectMultipleXThreads(int nativeThreadIndex)
        {
            ThreadHelper.DetectMultipleXThreads(nativeThreadIndex, CollectionSizeForMaxThreads);
        }
    }

    
    internal static class ThreadHelper
    {
        private static readonly ConcurrentBag<int> s_ThreadIndicesSeen = new ConcurrentBag<int>();
        
        [BurstDiscard]
        public static void DetectMultipleXThreads(int nativeThreadIndex, int maxSize)
        {
            s_ThreadIndicesSeen.Add(nativeThreadIndex);
            Debug.Assert(s_ThreadIndicesSeen.Count <= maxSize, $"Seen {s_ThreadIndicesSeen.Count} when we should only have seen {maxSize}. Output is: {GenerateOutput()}");
        }

        [BurstDiscard]
        private static string GenerateOutput()
        {
            string output = s_ThreadIndicesSeen.Aggregate(string.Empty, (current, index) => current + $"{index},");
            return output;
        }
    }
}
