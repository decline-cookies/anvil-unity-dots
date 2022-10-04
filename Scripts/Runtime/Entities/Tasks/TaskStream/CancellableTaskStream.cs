namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a stream of data for use in the task system via <see cref="AbstractTaskDriver"/> and/or
    /// <see cref="AbstractTaskSystem"/>. This stream also contains a secondary internal stream
    /// specifically for holding cancelled instances allowing them to trigger Cancel jobs.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> in the streams</typeparam>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CancellableTaskStream<TInstance> : TaskStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal readonly EntityProxyDataStream<TInstance> PendingCancelDataStream;

        internal sealed override bool IsCancellable
        {
            get => true;
        }

        internal CancellableTaskStream()
        {
            PendingCancelDataStream = new EntityProxyDataStream<TInstance>();
        }

        internal sealed override AbstractEntityProxyDataStream GetPendingCancelDataStream()
        {
            return PendingCancelDataStream;
        }
    }
}
