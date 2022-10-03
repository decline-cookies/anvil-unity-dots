using Anvil.Unity.DOTS.Util;
using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal readonly struct JobConfigDataID : IEquatable<JobConfigDataID>
    {
        public static bool operator ==(JobConfigDataID lhs, JobConfigDataID rhs)
        {
            return lhs.Type == rhs.Type && lhs.Usage == rhs.Usage;
        }

        public static bool operator !=(JobConfigDataID lhs, JobConfigDataID rhs)
        {
            return !(lhs == rhs);
        }
        
        public Type Type
        {
            get;
        }

        public AbstractJobConfig.Usage Usage
        {
            get;
        }

        public JobConfigDataID(AbstractDataStream dataStream, AbstractJobConfig.Usage usage) : this(dataStream.Type, usage)
        {
        }

        public JobConfigDataID(Type type, AbstractJobConfig.Usage usage)
        {
            Type = type;
            Usage = usage;
        }

        public bool Equals(JobConfigDataID other)
        {
            return this == other;
        }
        public override bool Equals(object compare)
        {
            return compare is JobConfigDataID id && Equals(id);
        }

        public override int GetHashCode()
        {
            return HashCodeUtil.GetHashCode(Type.GetHashCode(), (int)Usage);
        }
    }
}
