using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class ProfilingStats : IDisposable
    {
        public readonly Type DataType;
        public readonly AbstractTaskDriver TaskDriver;
        public readonly AbstractTaskSystem TaskSystem;
        
        public ProfilingInfo ProfilingInfo;

        internal ProfilingStats(AbstractDataStream dataStream)
        {
            DataType = dataStream.Type;
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
