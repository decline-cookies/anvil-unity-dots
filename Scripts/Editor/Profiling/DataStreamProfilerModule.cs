using Anvil.Unity.DOTS.Entities.Tasks;
using System;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace Anvil.Unity.DOTS.Editor.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Data Streams")]
    public class DataStreamProfilerModule : ProfilerModule
    {
        private static readonly ProfilerCounterDescriptor[] COUNTERS = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(DataStreamProfilingUtil.COUNTER_INSTANCES_LIVE_COUNT, ProfilerCategory.Memory),
            new ProfilerCounterDescriptor(DataStreamProfilingUtil.COUNTER_INSTANCES_LIVE_CAPACITY, ProfilerCategory.Memory),
            new ProfilerCounterDescriptor(DataStreamProfilingUtil.COUNTER_INSTANCES_PENDING_CAPACITY, ProfilerCategory.Memory)
        };

        private static readonly string[] AUTO_ENABLED_CATEGORY_NAMES = new string[]
        {
            ProfilerCategory.Memory.Name
        };

        public DataStreamProfilerModule() : base(COUNTERS, ProfilerModuleChartType.Line, AUTO_ENABLED_CATEGORY_NAMES)
        {
            
        }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new DataStreamDetailsViewController(ProfilerWindow);
        }
    }
}
