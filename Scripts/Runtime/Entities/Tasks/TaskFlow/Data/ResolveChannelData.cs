namespace Anvil.Unity.DOTS.Entities
{
    internal class ResolveChannelData
    {
        public AbstractProxyDataStream DataStream
        {
            get;
        }

        public byte Context
        {
            get;
        }

        public ResolveChannelData(AbstractProxyDataStream dataStream, byte context)
        {
            DataStream = dataStream;
            Context = context;
        }
    }
}
