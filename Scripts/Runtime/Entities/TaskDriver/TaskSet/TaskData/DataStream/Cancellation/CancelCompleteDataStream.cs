namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteDataStream : EntityProxyDataStream<CancelComplete>
    {
        private const string UNIQUE_CONTEXT_IDENTIFIER = "CANCEL_COMPLETE";
        
        public CancelCompleteDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner, UNIQUE_CONTEXT_IDENTIFIER) { }
    }
}
