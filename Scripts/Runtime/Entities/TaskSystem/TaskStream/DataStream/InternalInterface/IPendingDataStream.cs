namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IPendingDataStream
    {
        public unsafe void* PendingWriterPointer { get; }
    }
}
