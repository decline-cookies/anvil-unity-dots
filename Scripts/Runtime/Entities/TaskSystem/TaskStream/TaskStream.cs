using Anvil.CSharp.Core;
using System;
using System.Diagnostics;

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
        private readonly string m_TypeString;
        
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

            Type type = GetType();

            //TODO: Extract to Anvil-CSharp Util method -Used in AbstractJobConfig as well
            m_TypeString = type.IsGenericType
                ? $"{type.Name[..^2]}<{type.GenericTypeArguments[0].Name}>"
                : type.Name;
            
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
            return m_TypeString;
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
    
    public abstract class AbstractTaskStream : AbstractAnvilBase
    {
        internal abstract bool IsCancellable { get; }
        internal abstract bool IsDataStreamAResolveTarget { get; }
        internal abstract AbstractEntityProxyDataStream GetDataStream();
        internal abstract AbstractEntityProxyDataStream GetPendingCancelDataStream();
    }
}
