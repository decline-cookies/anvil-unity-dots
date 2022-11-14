using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public static class DataStreamProfilingUtil
    {
        public class AggCounterForType
        {
            public readonly Type Type;
            public readonly string ReadableTypeName;
            public readonly string ReadableInstanceTypeName;
            public readonly long BytesPerInstance;
            public readonly string MNLiveInstances;
            public readonly string MNLiveCapacity;
            public readonly string MNPendingCapacity;
            public readonly string MNLiveInstanceBytes;
            public readonly string MNLiveCapacityBytes;
            public readonly string MNPendingCapacityBytes;

            public ProfilerCounterValue<int> LiveInstances;
            public ProfilerCounterValue<int> LiveCapacity;
            public ProfilerCounterValue<int> PendingCapacity;

            public ProfilerCounterValue<long> LiveInstancesBytes;
            public ProfilerCounterValue<long> LiveCapacityBytes;
            public ProfilerCounterValue<long> PendingCapacityBytes;

            public AggCounterForType(Type type, Type instanceType, long bytesPerInstance)
            {
                Type = type;
                ReadableInstanceTypeName = instanceType.GetReadableName();
                BytesPerInstance = bytesPerInstance;
                
                ReadableTypeName = Type.GetReadableName();
                MNLiveInstances = $"{ReadableTypeName}-LiveInstances";
                MNLiveCapacity = $"{ReadableTypeName}-LiveCapacity";
                MNPendingCapacity = $"{ReadableTypeName}-PendingCapacity";

                MNLiveInstanceBytes = $"{ReadableTypeName}-LiveInstanceBytes";
                MNLiveCapacityBytes = $"{ReadableTypeName}-LiveCapacityBytes";
                MNPendingCapacityBytes = $"{ReadableTypeName}-PendingCapacityBytes";

                LiveInstances = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                              MNLiveInstances,
                                                              ProfilerMarkerDataUnit.Count,
                                                              ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
                LiveCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                             MNLiveCapacity,
                                                             ProfilerMarkerDataUnit.Count,
                                                             ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
                PendingCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                                MNPendingCapacity,
                                                                ProfilerMarkerDataUnit.Count,
                                                                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

                LiveInstancesBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
                                                                    MNLiveInstanceBytes,
                                                                    ProfilerMarkerDataUnit.Bytes,
                                                                    ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
                LiveCapacityBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
                                                                   MNLiveCapacityBytes,
                                                                   ProfilerMarkerDataUnit.Bytes,
                                                                   ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
                PendingCapacityBytes = new ProfilerCounterValue<long>(ProfilerCategory.Memory,
                                                                      MNPendingCapacityBytes,
                                                                      ProfilerMarkerDataUnit.Bytes,
                                                                      ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
            }
        }

        public const string COUNTER_INSTANCES_LIVE_COUNT = "Live Instances";
        public const string COUNTER_INSTANCES_LIVE_CAPACITY = "Live Capacity";
        public const string COUNTER_INSTANCES_PENDING_CAPACITY = "Pending Capacity";

        public static ProfilerCounterValue<int> Debug_InstancesLiveCount = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                                                                         COUNTER_INSTANCES_LIVE_COUNT,
                                                                                                         ProfilerMarkerDataUnit.Count,
                                                                                                         ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static ProfilerCounterValue<int> Debug_InstancesLiveCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                                                                            COUNTER_INSTANCES_LIVE_CAPACITY,
                                                                                                            ProfilerMarkerDataUnit.Count,
                                                                                                            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static ProfilerCounterValue<int> Debug_InstancesPendingCapacity = new ProfilerCounterValue<int>(ProfilerCategory.Memory,
                                                                                                               COUNTER_INSTANCES_PENDING_CAPACITY,
                                                                                                               ProfilerMarkerDataUnit.Count,
                                                                                                               ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);


        public static readonly Dictionary<Type, AggCounterForType> StatsByType = new Dictionary<Type, AggCounterForType>();

        public static void RegisterProfilingStats(ProfilingStats profilingStats)
        {
            RegisterProfilingStatsByType(profilingStats);
        }

        private static void RegisterProfilingStatsByType(ProfilingStats profilingStats)
        {
            if (!StatsByType.TryGetValue(profilingStats.DataType, out AggCounterForType agg))
            {
                agg = new AggCounterForType(profilingStats.DataType, 
                                            profilingStats.InstanceType, 
                                            profilingStats.LiveBytesPerInstance);
                StatsByType.Add(profilingStats.DataType, agg);
            }
        }

        public static void UpdateStatsByType(ProfilingStats profilingStats)
        {
            AggCounterForType agg = StatsByType[profilingStats.DataType];
            agg.LiveCapacity.Value += profilingStats.ProfilingInfo.LiveCapacity;
            agg.LiveInstances.Value += profilingStats.ProfilingInfo.LiveInstances;
            agg.PendingCapacity.Value += profilingStats.ProfilingInfo.PendingCapacity;
            agg.LiveCapacityBytes.Value += profilingStats.ProfilingInfo.LiveCapacity * profilingStats.LiveBytesPerInstance;
            agg.LiveInstancesBytes.Value += profilingStats.ProfilingInfo.LiveInstances * profilingStats.LiveBytesPerInstance;
            agg.PendingCapacityBytes.Value += profilingStats.ProfilingInfo.PendingCapacity * profilingStats.PendingBytesPerInstance;
        }
    }
}
