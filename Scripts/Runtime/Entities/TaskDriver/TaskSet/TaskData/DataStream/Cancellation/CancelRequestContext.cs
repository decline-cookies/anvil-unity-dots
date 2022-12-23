namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal readonly struct CancelRequestContext
    {
        public readonly uint TaskSetOwnerID;
        public readonly uint ActiveID;
        public readonly bool HasCancellableData;
        public CancelRequestContext(uint taskSetOwnerID, uint activeID, bool hasCancellableData)
        {
            TaskSetOwnerID = taskSetOwnerID;
            ActiveID = activeID;
            HasCancellableData = hasCancellableData;
        }
    }
}
