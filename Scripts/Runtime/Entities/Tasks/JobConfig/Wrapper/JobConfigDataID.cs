using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct JobConfigDataID
    {
        public Type Type
        {
            get;
        }

        public JobConfig.Usage Usage
        {
            get;
        }

        public JobConfigDataID(AbstractProxyDataStream dataStream, JobConfig.Usage usage) : this(dataStream.Type, usage)
        {
        }

        public JobConfigDataID(Type type, JobConfig.Usage usage)
        {
            Type = type;
            Usage = usage;
        }
    }
}
