using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    public static class TaskUpdateJobForDeferExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************
        public static JobHandle Schedule<TJob, TInstance>(
            this TJob jobData,
            UpdateScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn = default)
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IEntityKeyedTask
        {
            return InternalSchedule(
                jobData,
                scheduleInfo,
                dependsOn,
                ScheduleMode.Single,
                int.MaxValue);
        }

        public static JobHandle ScheduleParallel<TJob, TInstance>(
            this TJob jobData,
            UpdateScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn = default)
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IEntityKeyedTask
        {
            return InternalSchedule(
                jobData,
                scheduleInfo,
                dependsOn,
                ScheduleMode.Parallel,
                scheduleInfo.BatchSize);
        }

        private static unsafe JobHandle InternalSchedule<TJob, TInstance>(
            TJob jobData,
            UpdateScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn,
            ScheduleMode scheduleMode,
            int batchSize)
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IEntityKeyedTask
        {
            IntPtr reflectionData = WrapperJobProducer<TJob, TInstance>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);

            WrapperJobStruct<TJob, TInstance> wrapperData = new WrapperJobStruct<TJob, TInstance>(
                ref jobData,
                scheduleInfo);

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref wrapperData),
                reflectionData,
                dependsOn,
                scheduleMode);

            dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(
                ref scheduleParameters,
                batchSize,
                scheduleInfo.DeferredNativeArrayScheduleInfo.BufferPtr,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                scheduleInfo.DeferredNativeArrayScheduleInfo.SafetyHandlePtr
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

        internal struct WrapperJobStruct<TJob, TInstance>
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IEntityKeyedTask
        {
            private const int UNSET_NATIVE_THREAD_INDEX = -1;

            internal TJob JobData;
            internal DataStreamUpdater<TInstance> Updater;
            [NativeSetThreadIndex] internal readonly int NativeThreadIndex;

            public WrapperJobStruct(ref TJob jobData, UpdateScheduleInfo<TInstance> scheduleInfo)
            {
                JobData = jobData;
                Updater = scheduleInfo.Updater;
                NativeThreadIndex = UNSET_NATIVE_THREAD_INDEX;
            }
        }

        //*************************************************************************************************************
        // PRODUCER
        //*************************************************************************************************************
        private struct WrapperJobProducer<TJob, TInstance>
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IEntityKeyedTask
        {
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly IntPtr JOB_REFLECTION_DATA = JobsUtility.CreateJobReflectionData(
                typeof(WrapperJobStruct<TJob, TInstance>),
                typeof(TJob),
                (ExecuteJobFunction)Execute);


            private delegate void ExecuteJobFunction(
                ref WrapperJobStruct<TJob, TInstance> jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static unsafe void Execute(
                ref WrapperJobStruct<TJob, TInstance> wrapperData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                ref TJob jobData = ref wrapperData.JobData;
                ref DataStreamUpdater<TInstance> updater = ref wrapperData.Updater;

                updater.InitForThread(wrapperData.NativeThreadIndex);
                jobData.InitForThread(wrapperData.NativeThreadIndex);

                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int beginIndex, out int endIndex))
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), beginIndex, endIndex - beginIndex);
#endif

                    for (int i = beginIndex; i < endIndex; ++i)
                    {
                        jobData.Execute(updater[i], ref updater);
                    }
                }
            }
        }
    }
}