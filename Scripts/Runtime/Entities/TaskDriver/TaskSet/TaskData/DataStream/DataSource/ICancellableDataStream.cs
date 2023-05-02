using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal interface ICancellableDataStream
    {
        public uint PendingCancelActiveID { get; }
        
        public Type InstanceType { get; }
    }
}
