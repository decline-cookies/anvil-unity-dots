namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteDataStream : EntityProxyDataStream<CancelComplete>
    {
        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
        }
    }
}
