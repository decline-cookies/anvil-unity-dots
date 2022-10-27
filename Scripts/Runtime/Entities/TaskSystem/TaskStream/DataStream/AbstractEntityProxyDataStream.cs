using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractEntityProxyDataStream : AbstractDataStream
    {
        internal static readonly BulkScheduleDelegate<AbstractEntityProxyDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractEntityProxyDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);
        
        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
        internal abstract unsafe void* GetWriterPointer();
    }
}
