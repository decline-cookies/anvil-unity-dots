using Anvil.CSharp.Reflection;
using System;
using System.Diagnostics;
using Anvil.CSharp.Logging;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a stream of data for use in the task system via <see cref="AbstractTaskDriver"/> and/or
    /// <see cref="AbstractTaskSystem"/>.
    /// </summary>
    /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> in this stream</typeparam>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TaskStream<TInstance> : AbstractTaskStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        internal EntityProxyDataStream<TInstance> DataStream { get; }
        internal EntityProxyDataStream<TInstance> PendingCancelDataStream { get; }
        internal TaskStreamFlags Flags { get; }


        internal sealed override bool IsCancellable
        {
            get => (Flags & TaskStreamFlags.IsCancellable) != 0;
        }

        internal sealed override bool IsDataStreamAResolveTarget
        {
            get => (Flags & TaskStreamFlags.IsResolveTarget) != 0;
        }

        internal TaskStream(TaskStreamFlags flags)
        {
            Flags = flags;

            DataStream = new EntityProxyDataStream<TInstance>();

            if (IsCancellable)
            {
                PendingCancelDataStream = new EntityProxyDataStream<TInstance>();
            }
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            PendingCancelDataStream?.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return GetType().GetReadableName();
        }

        internal sealed override AbstractEntityProxyDataStream GetDataStream()
        {
            return DataStream;
        }

        internal sealed override AbstractEntityProxyDataStream GetPendingCancelDataStream()
        {
            Debug_EnsureCancellable();
            return PendingCancelDataStream;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCancellable()
        {
            if (!IsCancellable)
            {
                throw new NotSupportedException($"Tried to get Pending Cancel Data Stream on {this} but it doesn't exists. Check {IsCancellable} first!");
            }
        }
    }
}