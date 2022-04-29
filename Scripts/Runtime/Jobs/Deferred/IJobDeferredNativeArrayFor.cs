using Anvil.Unity.DOTS.Collections;
using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    [JobProducerType(typeof(JobDeferredNativeArrayForExtensions.JobDeferredNativeArrayForProducer<>))]
    public interface IJobDeferredNativeArrayFor
    {
        void Execute(int index);
    }

    public static class JobDeferredNativeArrayForExtensions
    {
        public static unsafe JobHandle ScheduleParallel<TJob, T>(this TJob jobData,
                                                                 DeferredNativeArray<T> deferredNativeArray,
                                                                 int batchSize,
                                                                 JobHandle dependsOn = default)
            where TJob : struct, IJobDeferredNativeArrayFor
            where T : struct
        {
            void* atomicSafetyHandlePtr = DeferredNativeArrayUnsafeUtility.GetSafetyHandlePointer(ref deferredNativeArray);
            

#if UNITY_2020_2_OR_NEWER
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Parallel;
#else
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Batched;
#endif

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData),
                                                                                                         JobDeferredNativeArrayForProducer<TJob>.Initialize(true),
                                                                                                         dependsOn,
                                                                                                         SCHEDULE_MODE);
            dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParameters,
                                                                      batchSize,
                                                                      DeferredNativeArrayUnsafeUtility.GetBufferInfoUnchecked(ref deferredNativeArray),
                                                                      atomicSafetyHandlePtr);

            // dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParameters, batchSize,);
            return dependsOn;
        }


        internal struct JobDeferredNativeArrayForProducer<TJob>
            where TJob : struct, IJobDeferredNativeArrayFor
        {
            // ReSharper disable once StaticMemberInGenericType
            private static IntPtr s_JobReflectionData;

            public static IntPtr Initialize(bool asParallel)
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobDeferredNativeArrayProducer<TJob, T>),
                                                                              typeof(TJob),
                                                                              (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(TJob),
                                                                              typeof(TJob),
                                                                              (asParallel)
                                                                                  ? JobType.ParallelFor
                                                                                  : JobType.Single,
                                                                              (ExecuteJobFunction)Execute);
#endif
                }

                return s_JobReflectionData;
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

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), beginIndex, endIndex - beginIndex);

                    for (int index = beginIndex; index < endIndex; ++index)
                    {
                        jobData.Execute(index);
                    }
                }
            }
        }
    }
}
