using Anvil.Unity.DOTS.Util;
using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal readonly struct JobConfigDataID : IEquatable<JobConfigDataID>
    {
        public static bool operator ==(JobConfigDataID lhs, JobConfigDataID rhs)
        {
            return lhs.AccessWrapperType == rhs.AccessWrapperType && lhs.Usage == rhs.Usage;
        }

        public static bool operator !=(JobConfigDataID lhs, JobConfigDataID rhs)
        {
            return !(lhs == rhs);
        }

        public Type AccessWrapperType { get; }

        public AbstractJobConfig.Usage Usage { get; }

        public JobConfigDataID(Type accessWrapperType, AbstractJobConfig.Usage usage)
        {
            AccessWrapperType = accessWrapperType;
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
            return HashCodeUtil.GetHashCode(AccessWrapperType.GetHashCode(), (int)Usage);
        }
    }
}