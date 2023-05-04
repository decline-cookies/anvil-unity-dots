namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal readonly struct CancelRequestContext
    {
        public readonly TaskSetOwnerID TaskSetOwnerID;
        public readonly DataTargetID DataTargetID;

        public CancelRequestContext(TaskSetOwnerID taskSetOwnerID, DataTargetID dataTargetID)
        {
            TaskSetOwnerID = taskSetOwnerID;
            DataTargetID = dataTargetID;
        }

        public override string ToString()
        {
            return $"TaskSetOwnerID: {TaskSetOwnerID}, DataTargetID: {DataTargetID}";
        }
    }
}
