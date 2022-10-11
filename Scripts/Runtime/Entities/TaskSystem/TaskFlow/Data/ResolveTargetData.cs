namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ResolveTargetData
    {
        public AbstractEntityProxyDataStream DataStream { get; }
        public byte Context { get; }

        public ResolveTargetData(AbstractEntityProxyDataStream dataStream, byte context)
        {
            DataStream = dataStream;
            Context = context;
        }
    }
}
