using Anvil.Unity.DOTS.Data;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public static class TaskUpdateJobForDeferExtension
    {
        //*************************************************************************************************************
        // SCHEDULING
        //*************************************************************************************************************

        public static unsafe JobHandle ScheduleParallel<TJob, TInstance>(this TJob jobData,
                                                                         DeferredNativeArrayScheduleInfo scheduleInfo,
                                                                         int batchSize,
                                                                         JobHandle dependsOn = default)
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IProxyInstance
        {
            void* atomicSafetyHandlePtr = null;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            atomicSafetyHandlePtr = scheduleInfo.SafetyHandlePtr;
#endif

            IntPtr reflectionData = WrapperJobProducer<TJob, TInstance>.JOB_REFLECTION_DATA;
            ValidateReflectionData(reflectionData);
            
            //TODO: Pass in the structs needed for filtering from the UpdateJobScheduleInfo into this wrapper struct
            WrapperJobStruct<TJob, TInstance> wrapperData = new WrapperJobStruct<TJob, TInstance>(ref jobData);

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
            where TInstance : unmanaged, IProxyInstance
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
        private struct WrapperJobProducer<TJob, TInstance>
            where TJob : struct, ITaskUpdateJobForDefer<TInstance>
            where TInstance : unmanaged, IProxyInstance
        {
            // ReSharper disable once StaticMemberInGenericType
            internal static readonly IntPtr JOB_REFLECTION_DATA = JobsUtility.CreateJobReflectionData(typeof(WrapperJobStruct<TJob, TInstance>),
                                                                                                     typeof(TJob),
                                                                                                     (ExecuteJobFunction)Execute);
            

            private delegate void ExecuteJobFunction(ref WrapperJobStruct<TJob, TInstance> jobData,
                                                     IntPtr additionalPtr,
                                                     IntPtr bufferRangePatchData,
                                                     ref JobRanges ranges,
                                                     int jobIndex);


            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by Burst.")]
            public static unsafe void Execute(ref WrapperJobStruct<TJob, TInstance> wrapperData,
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