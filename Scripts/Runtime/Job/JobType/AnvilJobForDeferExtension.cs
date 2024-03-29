using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class AnvilJobForDeferExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************

        public static JobHandle Schedule<TJob>(
            this TJob jobData,
            DeferredNativeArrayScheduleInfo scheduleInfo,
            JobHandle dependsOn = default)
            where TJob : struct, IAnvilJobForDefer
        {
            return InternalSchedule(
                jobData,
                scheduleInfo,
                dependsOn,
                ScheduleMode.Single,
                int.MaxValue);
        }

        public static JobHandle ScheduleParallel<TJob>(
            this TJob jobData,
            DeferredNativeArrayScheduleInfo scheduleInfo,
            int batchSize,
            JobHandle dependsOn = default)
            where TJob : struct, IAnvilJobForDefer
        {
            return InternalSchedule(
                jobData,
                scheduleInfo,
                dependsOn,
                ScheduleMode.Parallel,
                batchSize);
        }

        private static unsafe JobHandle InternalSchedule<TJob>(
            this TJob jobData,
            DeferredNativeArrayScheduleInfo scheduleInfo,
            JobHandle dependsOn,
            ScheduleMode scheduleMode,
            int batchSize)
            where TJob : struct, IAnvilJobForDefer
        {
            IntPtr reflectionData = WrapperJobProducer<TJob>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);

            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref wrapperData),
                reflectionData,
                dependsOn,
                scheduleMode);

            dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(
                ref scheduleParameters,
                batchSize,
                scheduleInfo.BufferPtr,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                scheduleInfo.SafetyHandlePtr
#else
                                                                      null
#endif
            );

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
            where TJob : struct, IAnvilJobForDefer
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
            where TJob : struct, IAnvilJobForDefer
        {
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly IntPtr JOB_REFLECTION_DATA = JobsUtility.CreateJobReflectionData(
                typeof(WrapperJobStruct<TJob>),
                typeof(TJob),
                (ExecuteJobFunction)Execute);


            private delegate void ExecuteJobFunction(
                ref WrapperJobStruct<TJob> jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static unsafe void Execute(
                ref WrapperJobStruct<TJob> wrapperData,
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
                    JobsUtility.PatchBufferMinMaxRanges(
                        bufferRangePatchData,
                        UnsafeUtility.AddressOf(ref jobData),
                        beginIndex,
                        endIndex - beginIndex);
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