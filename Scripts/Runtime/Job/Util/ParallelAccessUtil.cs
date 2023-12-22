using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Jobs;

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
using System.Collections.Concurrent;
using System.Linq;
#endif

namespace Anvil.Unity.DOTS.Jobs
{
    //TODO: #299 - Might be able to get rid of this class altogether.
    /// <summary>
    /// Utility methods for working accessing values in parallel.
    /// </summary>
    public static class ParallelAccessUtil
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
        /// <see cref="Init"/> method which then assigns to a <see cref="SharedStatic{int}"/>
        ///
        /// While a bit hacky, it is ensured to run before any potential jobs execute. The external call is allowed
        /// and we get the correct value and we can assign it to the <see cref="SharedStatic{int}"/>.
        /// Subsequent bursted jobs are then allowed to access it.
        /// <seealso cref="https://docs.unity3d.com/Packages/com.unity.burst@1.7/manual/docs/AdvancedUsages.html#shared-static"/>
        /// </remarks>
        private static readonly SharedStatic<int> JOB_WORKER_MAXIMUM_COUNT = SharedStatic<int>.GetOrCreate<JobWorkerMaximumCountKeyContext>();

        // ReSharper disable once ConvertToStaticClass
        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class JobWorkerMaximumCountKeyContext
        {
            private JobWorkerMaximumCountKeyContext() { }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            JOB_WORKER_MAXIMUM_COUNT.Data = JobsUtility.ThreadIndexCount;
            Debug.Assert(JOB_WORKER_MAXIMUM_COUNT.Data > 0);
        }


        /// <summary>
        /// Returns an ideal size for the number of buckets a collection should have
        /// to account for all possible threads that could write to it at once.
        /// </summary>
        /// <remarks>
        /// There is a lot of different terminology for the "buckets".
        /// <see cref="NativeStream"/> has foreachCount
        /// <see cref="UnsafeTypedStream{T}"/> has lanes
        /// etc
        /// It's the number of separate "buckets" that can be written to in parallel.
        /// </remarks>
        public static int CollectionSizeForMaxThreads
        {
            get => JOB_WORKER_MAXIMUM_COUNT.Data;
        }

        /// <summary>
        /// Returns the correct index for the collection based on the <paramref name="nativeThreadIndex"/> passed in.
        /// </summary>
        /// <remarks>
        /// Temporary, this used to do a lot more work but due to Entities 1.0, we can simply return the nativeThreadIndex.
        /// See: #299 for more details.
        /// </remarks>
        /// <param name="nativeThreadIndex">
        /// The native thread index to figure out the collection index from. Must be greater than 0.
        /// </param>
        /// <returns>The collection index to use</returns>
        public static int CollectionIndexForThread(int nativeThreadIndex)
        {
            DetectMultipleXThreads(nativeThreadIndex);
            Debug_EnsureNativeThreadIndexIsValid(nativeThreadIndex);
            return nativeThreadIndex;
        }

        /// <summary>
        /// Returns the index to use when operating on the main thread
        /// </summary>
        /// <remarks>
        /// This function assumes that the collection being used was sized appropriately via
        /// <see cref="ParallelAccessUtil.CollectionSizeForMaxThreads"/>.
        /// </remarks>
        /// <returns>The collection index to use</returns>
        public static int CollectionIndexForMainThread()
        {
            int mainThreadIndex = JobsUtility.ThreadIndex;
            DetectMultipleXThreads(mainThreadIndex);
            return mainThreadIndex;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private static void Debug_EnsureNativeThreadIndexIsValid(int nativeThreadIndex)
        {
            if (nativeThreadIndex < 0 || nativeThreadIndex > JOB_WORKER_MAXIMUM_COUNT.Data)
            {
                throw new InvalidOperationException($"Native Thread Index is {nativeThreadIndex}! Did you call {nameof(CollectionIndexForThread)} instead of {nameof(CollectionIndexForMainThread)}?");
            }
        }


        [BurstDiscard]
        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private static void DetectMultipleXThreads(int nativeThreadIndex)
        {
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            ThreadHelper.DetectMultipleXThreads(nativeThreadIndex, JOB_WORKER_MAXIMUM_COUNT.Data);
#endif
        }
    }

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
    internal static class ThreadHelper
    {
        private static readonly ConcurrentDictionary<int, bool> s_ThreadIndicesSeen = new ConcurrentDictionary<int, bool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_ThreadIndicesSeen.Clear();
        }

        //TODO: #84 - This is actually never getting called when in Burst. BurstDiscard removes when in Burst. Fix.
        [BurstDiscard]
        public static void DetectMultipleXThreads(int nativeThreadIndex, int maxSize)
        {
            s_ThreadIndicesSeen.TryAdd(nativeThreadIndex, true);
            if (s_ThreadIndicesSeen.Count > maxSize)
            {
                throw new InvalidOperationException($"Seen {s_ThreadIndicesSeen.Count} when we should only have seen {maxSize}. Output is: {GenerateOutput()}");
            }
        }

        [BurstDiscard]
        private static string GenerateOutput()
        {
            string output = s_ThreadIndicesSeen.Aggregate(string.Empty, (current, index) => current + $"{index},");
            return output;
        }
    }
#endif
}