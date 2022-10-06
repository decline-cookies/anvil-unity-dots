using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a stream of data for use in the task system via <see cref="AbstractTaskDriver"/> and/or
    /// <see cref="AbstractTaskSystem"/>.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> in this stream</typeparam>
    public class TaskStream<TInstance> : AbstractTaskStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal EntityProxyDataStream<TInstance> DataStream { get; }

        internal override bool IsCancellable
        {
            get => false;
        }

        internal TaskStream()
        {
            //TODO: #71 - This is weird with no dispose but #71 will fix it.
            DataStream = new EntityProxyDataStream<TInstance>();
        }

        internal sealed override AbstractEntityProxyDataStream GetDataStream()
        {
            return DataStream;
        }

        internal override AbstractEntityProxyDataStream GetPendingCancelDataStream()
        {
            throw new NotSupportedException($"Tried to get Pending Cancel Data Stream on {this} but it doesn't exists. Check {IsCancellable} first!");
        }
    }
}
