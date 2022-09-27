using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class CancelRequestsAccessWrapper : DataStreamAccessWrapper
    {
        public byte Context { get; }
        public CancelRequestsAccessWrapper(AbstractProxyDataStream dataStream, AccessType accessType, byte context) : base(dataStream, accessType)
        {
            Context = context;
        }
    }
}
