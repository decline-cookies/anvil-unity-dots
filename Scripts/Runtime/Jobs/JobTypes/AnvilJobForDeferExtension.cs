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
    public static class AnvilJobForDeferExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************
        public static unsafe JobHandle ScheduleParallel<TJob>(this TJob jobData,
                                                              DeferredNativeArrayScheduleInfo scheduleInfo,
                                                              int batchSize,
                                                              JobHandle dependsOn = default)
            where TJob : struct, IAnvilJobForDefer
        {
            void* atomicSafetyHandlePtr = null;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            atomicSafetyHandlePtr = scheduleInfo.SafetyHandlePtr;
#endif

            IntPtr reflectionData = WrapperJobProducer<TJob>.GetReflectionData();

            WrapperJobStruct<TJob> wrapperData = new WrapperJobStruct<TJob>(ref jobData);

            JobsUtility.JobScheduleParameters scheduleParameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref wrapperData),
                                                                                                         reflectionData,
                                                                                                         dependsOn,
                                                                                                         ScheduleMode.Parallel);


            dependsOn = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParameters,
                                                                      batchSize,
                                                                      scheduleInfo.BufferPtr,
                                                                      atomicSafetyHandlePtr);

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
            where TJob : struct, IAnvilJobForDefer
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
        internal struct WrapperJobProducer<TJob>
            where TJob : struct, IAnvilJobForDefer
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
            public static unsafe void Execute(ref WrapperJobStruct<TJob> wrapperData,
                                              IntPtr additionalPtr,
                                              IntPtr bufferRangePatchData,
                                              ref JobRanges ranges,
                                              int jobIndex)
            {
                ref TJob jobData = ref wrapperData.JobData;
                jobData.InitForThread(wrapperData.NativeThreadIndex);

                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int beginIndex, out int endIndex))
                    {
                        return;
                    }

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
