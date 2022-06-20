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
    public static class AnvilJobExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData,
                                                      JobHandle dependsOn = default)
            where TJob : struct, IAnvilJob
        {
            IntPtr reflectionData = WrapperJobProducer<TJob>.GetReflectionData();
            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);


            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref wrapperData),
                                                                                                         reflectionData,
                                                                                                         dependsOn,
                                                                                                         ScheduleMode.Single);

            dependsOn = JobsUtility.Schedule(ref scheduleParameters);
            return dependsOn;
        }
        
        //*************************************************************************************************************
        // STATIC HELPERS
        //*************************************************************************************************************

        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// Called by Unity automatically
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        public static void EarlyJobInit<TJob>()
            where TJob : struct, IAnvilJob
        {
            WrapperJobProducer<TJob>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
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
        
        internal struct WrapperJobProducer<TJob>
            where TJob : struct, IAnvilJob
        {
            // ReSharper disable once StaticMemberInGenericType
            private static readonly SharedStatic<IntPtr> JOB_REFLECTION_DATA = SharedStatic<IntPtr>.GetOrCreate<WrapperJobStruct<TJob>>();

            internal static IntPtr GetReflectionData()
            {
                IntPtr reflectionData = JOB_REFLECTION_DATA.Data;
                CheckReflectionDataCorrect(reflectionData);
                return reflectionData;
            }

            [Preserve]
            internal static void Initialize()
            {
                JOB_REFLECTION_DATA.Data = JobsUtility.CreateJobReflectionData(typeof(WrapperJobStruct<TJob>),
                                                                               typeof(TJob),
                                                                               (ExecuteJobFunction)Execute);
            }

            private delegate void ExecuteJobFunction(ref WrapperJobStruct<TJob> jobData,
                                                     IntPtr additionalPtr,
                                                     IntPtr bufferRangePatchData,
                                                     ref JobRanges ranges,
                                                     int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static void Execute(ref WrapperJobStruct<TJob> wrapperData,
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
