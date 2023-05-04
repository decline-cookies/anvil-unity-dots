namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal readonly struct ResolveTargetWriteData
    {
        public readonly long PendingWriterPointerAddress;
        public readonly DataTargetID DataTargetID;

        public ResolveTargetWriteData(long pendingWriterPointerAddress, DataTargetID dataTargetID)
        {
            PendingWriterPointerAddress = pendingWriterPointerAddress;
            DataTargetID = dataTargetID;
        }
    }
}
