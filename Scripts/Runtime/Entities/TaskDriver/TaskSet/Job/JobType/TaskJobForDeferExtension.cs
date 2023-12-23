using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    public static class TaskJobForDeferExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************

        public static JobHandle Schedule<TJob, TInstance>(
            this TJob jobData,
            DataStreamScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn = default)
            where TJob : unmanaged, ITaskJobForDefer
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
            DataStreamScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn = default)
            where TJob : unmanaged, ITaskJobForDefer
            where TInstance : unmanaged, IEntityKeyedTask
        {
            return InternalSchedule(
                jobData,
                scheduleInfo,
                dependsOn,
                ScheduleMode.Parallel,
                scheduleInfo.BatchSize);
        }

        public static unsafe JobHandle InternalSchedule<TJob, TInstance>(
            this TJob jobData,
            DataStreamScheduleInfo<TInstance> scheduleInfo,
            JobHandle dependsOn,
            ScheduleMode scheduleMode,
            int batchSize)
            where TJob : unmanaged, ITaskJobForDefer
            where TInstance : unmanaged, IEntityKeyedTask
        {
            WrapperJobProducer<TJob> wrapperData = new WrapperJobProducer<TJob>(ref jobData);

            IntPtr reflectionData = GetReflectionData<TJob>();
            ValidateReflectionData(reflectionData);

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
        // PRODUCER
        //*************************************************************************************************************

        [UsedImplicitly]
        public static void EarlyJobInit<TJob>()
            where TJob : unmanaged, ITaskJobForDefer
        {
            WrapperJobProducer<TJob>.Initialize();
        }

        private static IntPtr GetReflectionData<TJob>()
            where TJob : unmanaged, ITaskJobForDefer
        {
            WrapperJobProducer<TJob>.Initialize();
            IntPtr reflectionData = WrapperJobProducer<TJob>.JOB_REFLECTION_DATA.Data;
            // CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        internal struct WrapperJobProducer<TJob>
            where TJob : unmanaged, ITaskJobForDefer
        {
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly SharedStatic<IntPtr> JOB_REFLECTION_DATA = SharedStatic<IntPtr>.GetOrCreate<WrapperJobProducer<TJob>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (JOB_REFLECTION_DATA.Data == IntPtr.Zero)
                {
                    JOB_REFLECTION_DATA.Data = JobsUtility.CreateJobReflectionData(
                        typeof(WrapperJobProducer<TJob>),
                        typeof(TJob),
                        (ExecuteJobFunction)Execute);
                }
            }

            private delegate void ExecuteJobFunction(
                ref WrapperJobProducer<TJob> jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            private const int UNSET_NATIVE_THREAD_INDEX = -1;

            private TJob m_JobData;
            [NativeSetThreadIndex] internal readonly int NativeThreadIndex;

            public WrapperJobProducer(ref TJob jobData)
            {
                m_JobData = jobData;
                NativeThreadIndex = UNSET_NATIVE_THREAD_INDEX;
            }


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            internal static unsafe void Execute(
                ref WrapperJobProducer<TJob> wrapperData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                ref TJob jobData = ref wrapperData.m_JobData;

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