using System;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskStream
    {
        private readonly string m_TypeString;
        
        internal abstract bool IsCancellable { get; }
        internal abstract AbstractProxyDataStream GetDataStream();
        internal abstract AbstractProxyDataStream GetPendingCancelDataStream();

        protected AbstractTaskStream()
        {
            Type type = GetType();
            
            //TODO: Extract to Anvil-CSharp Util method -Used in AbstractJobConfig as well
            m_TypeString = type.IsGenericType
                ? $"{type.Name[..^2]}<{type.GenericTypeArguments[0].Name}>"
                : type.Name;
        }

        public override string ToString()
        {
            return m_TypeString;
        }
    }
}
