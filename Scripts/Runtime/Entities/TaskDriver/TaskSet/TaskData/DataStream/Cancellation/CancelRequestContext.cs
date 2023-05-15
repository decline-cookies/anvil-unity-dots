namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal readonly struct CancelRequestContext
    {
        public readonly DataOwnerID DataOwnerID;
        public readonly DataTargetID DataTargetID;

        public CancelRequestContext(DataOwnerID dataOwnerID, DataTargetID dataTargetID)
        {
            DataOwnerID = dataOwnerID;
            DataTargetID = dataTargetID;
        }

        public override string ToString()
        {
            return $"DataOwnerID: {DataOwnerID}, DataTargetID: {DataTargetID}";
        }
    }
}
