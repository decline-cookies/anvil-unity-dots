namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ResolveTargetData
    {
        public AbstractTypedDataStream<> DataStream { get; }
        public byte Context { get; }

        public ResolveTargetData(AbstractTypedDataStream<> dataStream, byte context)
        {
            DataStream = dataStream;
            Context = context;
        }
    }
}
