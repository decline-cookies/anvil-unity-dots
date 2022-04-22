using System;
using System.Reflection;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

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
        /// <see cref="ParallelCollectionUtil.Init"/> method which uses reflection to assign the static readonly
        /// variable.
        ///
        /// While a bit hacky, it is ensured to run before any potential jobs execute. The external call is allowed
        /// and we get the correct value and inject it into the static readonly field. Subsequent bursted jobs are then
        /// allowed to access it.
        /// </remarks>
        private static readonly int JOB_WORKER_MAXIMUM_COUNT = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Type type = typeof(ParallelCollectionUtil);
            FieldInfo field = type.GetField(nameof(JOB_WORKER_MAXIMUM_COUNT), BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, JobsUtility.JobWorkerMaximumCount);
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
        /// <returns>The number of buckets a collection should have.</returns>
        public static int CollectionSizeForMaxThreads()
        {
            return JOB_WORKER_MAXIMUM_COUNT + 1;
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
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> + 2.
        /// 2. The scheduler may place your job on a profiler thread in the editor. In this case the native thread index
        /// will also be <see cref="JobsUtility.JobWorkerMaximumCount"/> + 2.
        /// It is unknown why this is. 
        ///
        /// Our goal is to have a tightly packed collection so in the example of an 8 core machine with
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> equal to 15, we would have the following mapping.
        ///
        /// thread 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17 
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
            // JobsUtility.JobWorkerMaximumCount = 15
            // index  0, 1, 2, 3, 4, 5, 6, 7, 8, 9,  10, 11, 12, 13, 14, 15
            // thread 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17
            return (nativeThreadIndex <= JOB_WORKER_MAXIMUM_COUNT)
                ? nativeThreadIndex - 1
                : JOB_WORKER_MAXIMUM_COUNT;
        }
    }
}
