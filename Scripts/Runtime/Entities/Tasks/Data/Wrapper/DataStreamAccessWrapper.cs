using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class DataStreamAccessWrapper
    {
        public AbstractProxyDataStream DataStream
        {
            get;
        }

        public AccessType AccessType
        {
            get;
        }

        public DataStreamAccessWrapper(AbstractProxyDataStream dataStream, AccessType accessType)
        {
            DataStream = dataStream;
            AccessType = accessType;
        }
    }
}
