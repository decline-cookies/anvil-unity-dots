using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class JobDeferredNativeArrayForExtension
    {
        public static unsafe JobHandle ScheduleParallel<TJob, T>(this TJob jobData,
                                                                 DeferredNativeArray<T> deferredNativeArray,
                                                                 int batchSize,
                                                                 JobHandle dependsOn = default)
            where TJob : struct, IJobDeferredNativeArrayFor
            where T : struct
        {
            void* atomicSafetyHandlePtr = null;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            atomicSafetyHandlePtr = DeferredNativeArrayUnsafeUtility.GetSafetyHandlePointer(ref deferredNativeArray);
#endif

            IntPtr reflectionData = JobDeferredNativeArrayForProducer<TJob>.GetReflectionData();

#if UNITY_2020_2_OR_NEWER
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Parallel;
#else
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Batched;
#endif

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData),
                                                                                                         reflectionData,
                                                                                                         dependsOn,
                                                                                                         SCHEDULE_MODE);

            dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParameters,
                                                                      batchSize,
                                                                      DeferredNativeArrayUnsafeUtility.GetBufferInfoUnchecked(ref deferredNativeArray),
                                                                      atomicSafetyHandlePtr);

            return dependsOn;
        }


        internal struct JobDeferredNativeArrayForProducer<TJob>
            where TJob : struct, IJobDeferredNativeArrayFor
        {
            // ReSharper disable once StaticMemberInGenericType
            private static readonly SharedStatic<IntPtr> s_JobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobDeferredNativeArrayForProducer<TJob>>();

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckReflectionDataCorrect(IntPtr reflectionData)
            {
                if (reflectionData == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Reflection data was not set up by a call to Initialize()");
                }
            }

            internal static IntPtr GetReflectionData()
            {
                IntPtr reflectionData = s_JobReflectionData.Data;
                CheckReflectionDataCorrect(reflectionData);
                return reflectionData;
            }

            [Preserve]
            internal static void Initialize()
            {
                if (s_JobReflectionData.Data == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(TJob),
                                                                                   typeof(TJob),
                                                                                   (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(TJob),
                                                                              typeof(TJob),
                                                                              JobType.ParallelFor,
                                                                              (ExecuteJobFunction)Execute);
#endif
                }
            }

            private delegate void ExecuteJobFunction(ref TJob jobData,
                                                     IntPtr additionalPtr,
                                                     IntPtr bufferRangePatchData,
                                                     ref JobRanges ranges,
                                                     int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static unsafe void Execute(ref TJob jobData,
                                              IntPtr additionalPtr,
                                              IntPtr bufferRangePatchData,
                                              ref JobRanges ranges,
                                              int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int beginIndex, out int endIndex))
                    {
                        return;
                    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), beginIndex, endIndex - beginIndex);
#endif

                    for (int index = beginIndex; index < endIndex; ++index)
                    {
                        jobData.Execute(index);
                    }
                }
            }
        }

        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// Called by Unity automatically
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        public static void EarlyJobInit<TJob>()
            where TJob : struct, IJobDeferredNativeArrayFor
        {
            JobDeferredNativeArrayForProducer<TJob>.Initialize();
        }
    }
}
