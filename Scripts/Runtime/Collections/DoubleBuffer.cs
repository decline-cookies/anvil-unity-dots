using Anvil.CSharp.Core;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Collections
{
    //TODO: Probably need a better name here
    public class DoubleBuffer<T> : AbstractAnvilBase
        where T : struct, INativeDisposable
    {
        private Func<T> m_CreationFunction;
        private Action<T> m_DisposalFunction;

        public T Previous
        {
            get;
            private set;
        }
        
        public T Current
        {
            get;
            private set;
        }

        public DoubleBuffer(Func<T> creationFunction, Action<T> disposalFunction)
        {
            m_CreationFunction = creationFunction;
            m_DisposalFunction = disposalFunction;
            
            Current = m_CreationFunction();
        }

        protected override void DisposeSelf()
        {
            m_DisposalFunction(Previous);
            m_DisposalFunction(Current);

            m_CreationFunction = null;
            m_DisposalFunction = null;
            base.DisposeSelf();
        }
        
        //TODO: Better name here
        public void SwapBuffer()
        {
            Previous = Current;
            Current = m_CreationFunction();
        }
    }
}
