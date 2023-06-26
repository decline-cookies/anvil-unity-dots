using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a read only reference to a <see cref="IAbstractDataStream{TInstance}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <typeparam name="TInstance">They type of <see cref="IEntityKeyedTask"/> to read</typeparam>
    [BurstCompatible]
    public readonly struct DataStreamActiveReader<TInstance> : IEnumerable<TInstance>
        where TInstance : unmanaged, IEntityKeyedTask
    {
        [ReadOnly] private readonly NativeArray<EntityKeyedTaskWrapper<TInstance>> m_Active;

        internal DataStreamActiveReader(NativeArray<EntityKeyedTaskWrapper<TInstance>> active)
        {
            m_Active = active;
        }

        /// <summary>
        /// Gets the <typeparamref name="TInstance"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public TInstance this[int index]
        {
            get => m_Active[index].Payload;
        }

        /// <summary>
        /// Gets the length of the backing array.
        /// </summary>
        public int Length
        {
            get => m_Active.Length;
        }

        /// <inheritdoc cref="IEnumerable"/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<TInstance> GetEnumerator()
        {
            return new Enumerator(m_Active.GetEnumerator());
        }

        // ----- Enumerator ----- //
        internal struct Enumerator : IEnumerator<TInstance>, IEnumerator, IDisposable
        {
            private NativeArray<EntityKeyedTaskWrapper<TInstance>>.Enumerator m_InnerEnumerator;

            public Enumerator(NativeArray<EntityKeyedTaskWrapper<TInstance>>.Enumerator innerEnumerator)
            {
                m_InnerEnumerator = innerEnumerator;
            }

            public bool MoveNext()
            {
                return m_InnerEnumerator.MoveNext();
            }

            public void Reset()
            {
                m_InnerEnumerator.Reset();
            }

            object IEnumerator.Current
            {
                get => m_InnerEnumerator.Current.Payload;
            }

            public void Dispose()
            {
                m_InnerEnumerator.Dispose();
            }

            public TInstance Current
            {
                get => m_InnerEnumerator.Current.Payload;
            }
        }
    }
}