using Anvil.Unity.DOTS.Collections;
using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    [JobProducerType(typeof(JobDeferredNativeArrayExtensions.JobDeferredNativeArrayProducer<,>))]
    public interface IJobDeferredNativeArray<T>
        where T : struct
    {
        void Execute(DeferredNativeArray<T> readOnlyDeferredNativeArray);
    }

    public static class JobDeferredNativeArrayExtensions
    {
        public static unsafe JobHandle Schedule<TJob, T>(this TJob jobData,
                                                         DeferredNativeArray<T> deferredNativeArray,
                                                         JobHandle dependsOn)
            where TJob : struct, IJobDeferredNativeArray<T>
            where T : struct
        {
            JobDeferredNativeArrayProducer<TJob, T> wrapperData = new JobDeferredNativeArrayProducer<TJob, T>(jobData, deferredNativeArray);

#if UNITY_2020_2_OR_NEWER
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Parallel;
#else
            const ScheduleMode SCHEDULE_MODE = ScheduleMode.Batched;
#endif

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref wrapperData),
                                                                                                         JobDeferredNativeArrayProducer<TJob, T>.Initialize(),
                                                                                                         dependsOn,
                                                                                                         SCHEDULE_MODE);

            dependsOn = JobsUtility.Schedule(ref scheduleParameters);
            return dependsOn;
        }

        internal struct JobDeferredNativeArrayProducer<TJob, T>
            where TJob : struct, IJobDeferredNativeArray<T>
            where T : struct
        {
            // ReSharper disable once StaticMemberInGenericType
            private static IntPtr s_JobReflectionData;

            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobDeferredNativeArrayProducer<TJob, T>),
                                                                              typeof(TJob),
                                                                              (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobDeferredNativeArrayProducer<TJob, T>),
                                                                              typeof(TJob),
                                                                              JobType.Single,
                                                                              (ExecuteJobFunction)Execute);
#endif
                }

                return s_JobReflectionData;
            }

            private delegate void ExecuteJobFunction(ref JobDeferredNativeArrayProducer<TJob, T> wrapperData,
                                                     IntPtr additionalPtr,
                                                     IntPtr bufferRangePatchData,
                                                     ref JobRanges ranges,
                                                     int jobIndex);

            private TJob m_JobData;
            [ReadOnly] private readonly DeferredNativeArray<T> m_DeferredNativeArray;

            public JobDeferredNativeArrayProducer(TJob jobData, DeferredNativeArray<T> deferredNativeArray)
            {
                m_JobData = jobData;
                m_DeferredNativeArray = deferredNativeArray;
            }

            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static void Execute(ref JobDeferredNativeArrayProducer<TJob, T> wrapperData,
                                       IntPtr additionalPtr,
                                       IntPtr bufferRangePatchData,
                                       ref JobRanges ranges,
                                       int jobIndex)
            {
                wrapperData.m_JobData.Execute(wrapperData.m_DeferredNativeArray);
            }
        }
    }
}
