using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct JobConfigDataID
    {
        public Type Type
        {
            get;
        }

        public AbstractJobConfig.Usage Usage
        {
            get;
        }

        public JobConfigDataID(AbstractProxyDataStream dataStream, AbstractJobConfig.Usage usage) : this(dataStream.Type, usage)
        {
        }

        public JobConfigDataID(Type type, AbstractJobConfig.Usage usage)
        {
            Type = type;
            Usage = usage;
        }
    }
}
