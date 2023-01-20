namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal readonly struct ResolveTargetWriteData
    {
        public readonly long PendingWriterPointerAddress;
        public readonly uint ActiveID;

        public ResolveTargetWriteData(long pendingWriterPointerAddress, uint activeID)
        {
            PendingWriterPointerAddress = pendingWriterPointerAddress;
            ActiveID = activeID;
        }
    }
}
