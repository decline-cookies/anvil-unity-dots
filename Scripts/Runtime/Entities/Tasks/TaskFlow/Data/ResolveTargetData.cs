namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ResolveTargetData
    {
        public AbstractProxyDataStream DataStream
        {
            get;
        }

        public byte Context
        {
            get;
        }

        public ResolveTargetData(AbstractProxyDataStream dataStream, byte context)
        {
            DataStream = dataStream;
            Context = context;
        }
    }
}
