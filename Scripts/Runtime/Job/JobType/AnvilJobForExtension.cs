using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class AnvilJobForExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData,
                                                      int arrayLength,
                                                      JobHandle dependsOn = default)
            where TJob : struct, IAnvilJobFor
        {
            IntPtr reflectionData = WrapperJobProducer<TJob>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);
            
            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);


            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref wrapperData),
                                                                                                         reflectionData,
                                                                                                         dependsOn,
                                                                                                         ScheduleMode.Single);

            dependsOn = JobsUtility.ScheduleParallelFor(ref scheduleParameters, arrayLength, arrayLength);
            return dependsOn;
        }

        public static unsafe JobHandle ScheduleParallel<TJob>(this TJob jobData,
                                                              int arrayLength,
                                                              int innerloopBatchCount,
                                                              JobHandle dependsOn = default)
            where TJob : struct, IAnvilJobFor
        {
            IntPtr reflectionData = WrapperJobProducer<TJob>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);
            
            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);


            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref wrapperData),
                                                                                                         reflectionData,
                                                                                                         dependsOn,
                                                                                                         ScheduleMode.Parallel);

            dependsOn = JobsUtility.ScheduleParallelFor(ref scheduleParameters, arrayLength, innerloopBatchCount);
            return dependsOn;
        }

        //*************************************************************************************************************
        // STATIC HELPERS
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ValidateReflectionData(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
            {
                throw new InvalidOperationException("Reflection data was not set up by a call to Initialize()");
            }
        }

        //*************************************************************************************************************
        // WRAPPER STRUCT
        //*************************************************************************************************************

        internal struct WrapperJobStruct<TJob>
            where TJob : struct, IAnvilJobFor
        {
            private const int UNSET_NATIVE_THREAD_INDEX = -1;

            internal TJob JobData;
            [NativeSetThreadIndex] internal readonly int NativeThreadIndex;

            public WrapperJobStruct(ref TJob jobData)
            {
                JobData = jobData;
                NativeThreadIndex = UNSET_NATIVE_THREAD_INDEX;
            }
        }

        //*************************************************************************************************************
        // PRODUCER
        //*************************************************************************************************************
        private struct WrapperJobProducer<TJob>
            where TJob : struct, IAnvilJobFor
        {
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly IntPtr JOB_REFLECTION_DATA = JobsUtility.CreateJobReflectionData(typeof(WrapperJobStruct<TJob>),
                                                                                                      typeof(TJob),
                                                                                                      (ExecuteJobFunction)Execute);

            private delegate void ExecuteJobFunction(ref WrapperJobStruct<TJob> jobData,
                                                     IntPtr additionalPtr,
                                                     IntPtr bufferRangePatchData,
                                                     ref JobRanges ranges,
                                                     int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static unsafe void Execute(ref WrapperJobStruct<TJob> wrapperData,
                                              IntPtr additionalPtr,
                                              IntPtr bufferRangePatchData,
                                              ref JobRanges ranges,
                                              int jobIndex)
            {
                ref TJob jobData = ref wrapperData.JobData;
                jobData.InitForThread(wrapperData.NativeThreadIndex);

                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int beginIndex, out int endIndex))
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), beginIndex, endIndex - beginIndex);
#endif

                    for (int i = beginIndex; i < endIndex; ++i)
                    {
                        jobData.Execute(i);
                    }
                }
            }
        }
    }
}
