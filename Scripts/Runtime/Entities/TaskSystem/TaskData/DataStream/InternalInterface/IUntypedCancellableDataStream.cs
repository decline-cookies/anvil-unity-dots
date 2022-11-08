namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IUntypedCancellableDataStream : IArrayDataStream
    {
        public AbstractDataStream UntypedPendingCancelDataStream { get; }
    }
}
