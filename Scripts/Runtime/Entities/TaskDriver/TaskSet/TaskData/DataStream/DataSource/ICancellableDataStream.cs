using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal interface ICancellableDataStream
    {
        public DataTargetID CancelDataTargetID { get; }

        public Type InstanceType { get; }
    }
}
