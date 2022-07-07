using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForIterate<TKey> : AbstractVDWrapper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        public VDWrapperForIterate(AbstractVirtualData<TKey> data) : base(data)
        {
        }
        
        public override JobHandle AcquireAsync()
        {
            return Data.AccessController.AcquireAsync(AccessType.SharedRead);
        }

        public override void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            Data.AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public override void Acquire()
        {
            Data.AccessController.Acquire(AccessType.SharedRead);
        }

        public override void Release()
        {
            Data.AccessController.Release();
        }
    }
}
