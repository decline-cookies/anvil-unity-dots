// using Anvil.Unity.DOTS.Collections;
// using System;
// using Unity.Jobs;
// using Unity.Jobs.LowLevel.Unsafe;
//
// namespace Anvil.Unity.DOTS.Jobs
// {
//     [JobProducerType(typeof(IDeferredNativeArrayJobExtensions.DeferredNativeArrayJobProducer<>))]
//     public interface IDeferredNativeArrayJob
//     {
//         void Execute(int startIndex, int count);
//     }
//
//     public static class IDeferredNativeArrayJobExtensions
//     {
//         
//         public static unsafe JobHandle ScheduleBatch<T, TType>(this T jobData, DeferredNativeArray<TType> deferredNativeArray, int minBatchSize, JobHandle dependsOn)
//             where TType : struct
//         {
//                 
//         }
//         
//         internal struct DeferredNativeArrayJobProducer<T>
//             where T : struct, IDeferredNativeArrayJob
//         {
//             private static IntPtr s_JobReflectionData;
//
//             public static IntPtr Initialize()
//             {
//                 if (s_JobReflectionData == IntPtr.Zero)
//                 {
//                     s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T),
//                                                                               typeof(T),
//                                                                               JobType.ParallelFor,
//                                                                               (ExecuteJobFunction)Execute);
//                 }
//
//                 return s_JobReflectionData;
//             }
//
//             public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
//
//             public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
//             {
//                 while (true)
//                 {
//                     //TODO
//                 }
//             }
//         }
//     }
// }
