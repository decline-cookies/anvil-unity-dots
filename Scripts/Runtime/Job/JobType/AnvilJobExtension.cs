using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class AnvilJobExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************
        public static unsafe JobHandle Schedule<TJob>(
            this TJob jobData,
            JobHandle dependsOn = default)
            where TJob : struct, IAnvilJob
        {
            IntPtr reflectionData = WrapperJobProducer<TJob>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);

            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref wrapperData),
                reflectionData,
                dependsOn,
                ScheduleMode.Single);

            dependsOn = JobsUtility.Schedule(ref scheduleParameters);
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
            where TJob : struct, IAnvilJob
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
            where TJob : struct, IAnvilJob
        {
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
            public static void Execute(
                ref WrapperJobStruct<TJob> wrapperData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                ref TJob jobData = ref wrapperData.JobData;
                jobData.InitForThread(wrapperData.NativeThreadIndex);
                jobData.Execute();
            }
        }
    }
}
