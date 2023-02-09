namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteJobConfig : DataStreamJobConfig<CancelComplete>
    {
        public CancelCompleteJobConfig(ITaskSetOwner taskSetOwner, CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskSetOwner, cancelCompleteDataStream) { }
    }
}