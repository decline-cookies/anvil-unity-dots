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
            public readonly string MNLiveInstances;
            public readonly string MNLiveCapacity;
            public readonly string MNPendingCapacity;
            
            public ProfilerCounterValue<int> LiveInstances;
            public ProfilerCounterValue<int> LiveCapacity;
            public ProfilerCounterValue<int> PendingCapacity;

            public AggCounterForType(Type type)
            {
                Type = type;
                ReadableTypeName = Type.GetReadableName();
                MNLiveInstances = $"{ReadableTypeName}-LiveInstances";
                MNLiveCapacity = $"{ReadableTypeName}-LiveCapacity";
                MNPendingCapacity = $"{ReadableTypeName}-PendingCapacity";
                
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
                agg = new AggCounterForType(profilingStats.DataType);
                StatsByType.Add(profilingStats.DataType, agg);
            }
        }

        public static void UpdateStatsByType(ProfilingStats profilingStats)
        {
            AggCounterForType agg = StatsByType[profilingStats.DataType];
            agg.LiveCapacity.Value += profilingStats.ProfilingInfo.LiveCapacity;
            agg.LiveInstances.Value += profilingStats.ProfilingInfo.LiveInstances;
            agg.PendingCapacity.Value += profilingStats.ProfilingInfo.PendingCapacity;
        }
    }
}
