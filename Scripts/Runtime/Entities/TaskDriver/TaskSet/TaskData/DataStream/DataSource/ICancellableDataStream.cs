using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal interface ICancellableDataStream
    {
        public DataTargetID PendingCancelDataTargetID { get; }
        
        public Type InstanceType { get; }
    }
}
