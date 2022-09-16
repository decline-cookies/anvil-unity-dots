using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IProxyDataStream : IAnvilDisposable
    {
        internal static readonly BulkScheduleDelegate<IProxyDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<IProxyDataStream>(nameof(BulkConsolidateForFrame), BindingFlags.Static | BindingFlags.NonPublic);
        
        private static JobHandle BulkConsolidateForFrame(IProxyDataStream dataStream, JobHandle dependsOn)
        {
            return dataStream.ConsolidateForFrame(dependsOn);
        }

        public JobHandle ConsolidateForFrame(JobHandle jobHandle);
        
        public string DebugString
        {
            get;
        }
    }
}
