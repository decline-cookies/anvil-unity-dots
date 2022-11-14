using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class ProfilingStats : IDisposable
    {
        public readonly Type DataType;
        public readonly Type InstanceType;
        public readonly long PendingBytesPerInstance;
        public readonly long LiveBytesPerInstance;
        public readonly AbstractTaskDriver TaskDriver;
        public readonly AbstractTaskSystem TaskSystem;
        
        public ProfilingInfo ProfilingInfo;

        internal ProfilingStats(AbstractDataStream dataStream)
        {
            DataType = dataStream.Type;
            InstanceType = dataStream.Debug_InstanceType;
            PendingBytesPerInstance = dataStream.Debug_PendingBytesPerInstance;
            LiveBytesPerInstance = dataStream.Debug_LiveBytesPerInstance;
            TaskDriver = dataStream.OwningTaskDriver;
            TaskSystem = dataStream.OwningTaskSystem;
            ProfilingInfo = new ProfilingInfo(dataStream);
        }

        public void Dispose()
        {
            ProfilingInfo.Dispose();
        }
    }
}
