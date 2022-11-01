namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ResolveTargetData
    {
        public AbstractDataStream DataStream { get; }
        public byte Context { get; }

        public ResolveTargetData(AbstractDataStream dataStream, byte context)
        {
            DataStream = dataStream;
            Context = context;
        }
    }
}
