using System;
using System.Reflection;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    public static class ParallelCollectionUtil
    {
        private static readonly int JOB_WORKER_MAXIMUM_COUNT = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Type type = typeof(ParallelCollectionUtil);
            FieldInfo field = type.GetField(nameof(JOB_WORKER_MAXIMUM_COUNT), BindingFlags.Static | BindingFlags.NonPublic); 
            field.SetValue(null, JobsUtility.JobWorkerMaximumCount);
        }

        public static int CollectionIndexForThread(int nativeThreadIndex)
        {
            // JobsUtility.JobWorkerMaximumCount = 15
            // index  0, 1, 2, 3, 4, 5, 6, 7, 8, 9,  10, 11, 12, 13, 14, 15
            // thread 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17
            return (nativeThreadIndex <= JOB_WORKER_MAXIMUM_COUNT)
                ? nativeThreadIndex - 1
                : JOB_WORKER_MAXIMUM_COUNT;
        }

        public static int CollectionSizeForMaxThreads()
        {
            return JOB_WORKER_MAXIMUM_COUNT + 1;
        }
    }
}
