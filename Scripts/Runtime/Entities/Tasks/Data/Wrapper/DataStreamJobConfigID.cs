using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct DataStreamJobConfigID
    {
        public Type Type
        {
            get;
        }

        public JobConfig.Usage Usage
        {
            get;
        }

        public DataStreamJobConfigID(AbstractProxyDataStream dataStream, JobConfig.Usage usage)
        {
            Type = dataStream.GetType();
            Usage = usage;
        }

        public DataStreamJobConfigID(Type dataStreamType, JobConfig.Usage usage)
        {
            Type = dataStreamType;
            Usage = usage;
        }
    }
}
