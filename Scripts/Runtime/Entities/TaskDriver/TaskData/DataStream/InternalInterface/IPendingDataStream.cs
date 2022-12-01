namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IPendingDataStream : IAccessControlledDataStream
    {
        public unsafe void* PendingWriterPointer { get; }
    }
}
