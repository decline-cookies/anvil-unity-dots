// using Anvil.CSharp.Logging;
// using System;
// using System.Collections.Generic;
// using Unity.Profiling;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     //TODO: #108 - Rework this a bit to try and remove the need for things to be public since only the profiler 
//     //TODO: should be working with this.
//     /// <summary>
//     /// Helper class for profiling Data Streams
//     /// </summary>
//     public static class DataStreamProfilingUtil
//     {
//         /// <summary>
//         /// Specific profiling information for a <see cref="AbstractDataStream"/>
//         /// </summary>
//         public class ProfilingInfoForDataStreamType
//         {
//             public readonly Type Type;
//             public readonly string ReadableTypeName;
//             public readonly string ReadableInstanceTypeName;
//             public readonly long BytesPerInstance;
//             public readonly string MNLiveInstances;
//             public readonly string MNLiveCapacity;
//             public readonly string MNPendingCapacity;
//             public readonly string MNLiveInstanceBytes;
//             public readonly string MNLiveCapacityBytes;
//             public readonly string MNPendingCapacityBytes;
//
//             public ProfilerCounterValue<int> LiveInstances;
//             public ProfilerCounterValue<int> LiveCapacity;
//             public ProfilerCounterValue<int> PendingCapacity;
//
//             public ProfilerCounterValue<long> LiveInstancesBytes;
//             public ProfilerCounterValue<long> LiveCapacityBytes;
//             public ProfilerCounterValue<long> PendingCapacityBytes;
//
//             public ProfilingInfoForDataStreamType(Type type, Type instanceType, long bytesPerInstance)
//             {
//                 Type = type;
//                 ReadableInstanceTypeName = instanceType.GetReadableName();
//                 BytesPerInstance = bytesPerInstance;
//                 
//                 ReadableTypeName = Type.GetReadableName();
//                 MNLiveInstances = $"{ReadableTypeName}-LiveInstances";
//                 MNLiveCapacity = $"{ReadableTypeName}-LiveCapacity";
//                 MNPendingCapacity = $"{ReadableTypeName}-PendingCapacity";
//
//                 MNLiveInstanceBytes = $"{ReadableTypeName}-LiveInstanceBytes";
//                 MNLiveCapacityBytes = $"{ReadableTypeName}-LiveCapacityBytes";
//                 MNPendingCapacityBytes = $"{ReadableTypeName}-PendingCapacityBytes";
//
//                 LiveInstances = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                               MNLiveInstances,
//                                                               ProfilerMarkerDataUnit.Count,
//                                                               ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//                 LiveCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                              MNLiveCapacity,
//                                                              ProfilerMarkerDataUnit.Count,
//                                                              ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//                 PendingCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                                 MNPendingCapacity,
//                                                                 ProfilerMarkerDataUnit.Count,
//                                                                 ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//
//                 LiveInstancesBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
//                                                                     MNLiveInstanceBytes,
//                                                                     ProfilerMarkerDataUnit.Bytes,
//                                                                     ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//                 LiveCapacityBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
//                                                                    MNLiveCapacityBytes,
//                                                                    ProfilerMarkerDataUnit.Bytes,
//                                                                    ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//                 PendingCapacityBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
//                                                                       MNPendingCapacityBytes,
//                                                                       ProfilerMarkerDataUnit.Bytes,
//                                                                       ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//             }
//         }
//
//         public const string COUNTER_INSTANCES_LIVE_COUNT = "Live Instances";
//         public const string COUNTER_INSTANCES_LIVE_CAPACITY = "Live Capacity";
//         public const string COUNTER_INSTANCES_PENDING_CAPACITY = "Pending Capacity";
//
//         public static ProfilerCounterValue<int> Debug_InstancesLiveCount = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                                                                          COUNTER_INSTANCES_LIVE_COUNT,
//                                                                                                          ProfilerMarkerDataUnit.Count,
//                                                                                                          ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//
//         public static ProfilerCounterValue<int> Debug_InstancesLiveCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                                                                             COUNTER_INSTANCES_LIVE_CAPACITY,
//                                                                                                             ProfilerMarkerDataUnit.Count,
//                                                                                                             ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//
//         public static ProfilerCounterValue<int> Debug_InstancesPendingCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
//                                                                                                                COUNTER_INSTANCES_PENDING_CAPACITY,
//                                                                                                                ProfilerMarkerDataUnit.Count,
//                                                                                                                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//
//
//         public static readonly Dictionary<Type, ProfilingInfoForDataStreamType> StatsByType = new Dictionary<Type, ProfilingInfoForDataStreamType>();
//
//         public static void RegisterProfilingStats(DataStreamProfilingInfo profilingInfo)
//         {
//             RegisterProfilingStatsByType(profilingInfo);
//         }
//
//         private static void RegisterProfilingStatsByType(DataStreamProfilingInfo profilingInfo)
//         {
//             if (!StatsByType.TryGetValue(profilingInfo.DataType, out ProfilingInfoForDataStreamType agg))
//             {
//                 agg = new ProfilingInfoForDataStreamType(profilingInfo.DataType, 
//                                             profilingInfo.InstanceType, 
//                                             profilingInfo.LiveBytesPerInstance);
//                 StatsByType.Add(profilingInfo.DataType, agg);
//             }
//         }
//
//         public static void UpdateStatsByType(DataStreamProfilingInfo profilingInfo)
//         {
//             ProfilingInfoForDataStreamType agg = StatsByType[profilingInfo.DataType];
//             agg.LiveCapacity.Value += profilingInfo.ProfilingDetails.LiveCapacity;
//             agg.LiveInstances.Value += profilingInfo.ProfilingDetails.LiveInstances;
//             agg.PendingCapacity.Value += profilingInfo.ProfilingDetails.PendingCapacity;
//             agg.LiveCapacityBytes.Value += profilingInfo.ProfilingDetails.LiveCapacity * profilingInfo.LiveBytesPerInstance;
//             agg.LiveInstancesBytes.Value += profilingInfo.ProfilingDetails.LiveInstances * profilingInfo.LiveBytesPerInstance;
//             agg.PendingCapacityBytes.Value += profilingInfo.ProfilingDetails.PendingCapacity * profilingInfo.PendingBytesPerInstance;
//         }
//     }
// }
