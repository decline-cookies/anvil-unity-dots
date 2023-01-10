namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal readonly struct CancelRequestContext
    {
        public readonly uint TaskSetOwnerID;
        public readonly uint ActiveID;
        public CancelRequestContext(uint taskSetOwnerID, uint activeID)
        {
            TaskSetOwnerID = taskSetOwnerID;
            ActiveID = activeID;
        }

        public override string ToString()
        {
            return $"TaskSetOwnerID: {TaskSetOwnerID}, ActiveID: {ActiveID}";
        }
    }
}
