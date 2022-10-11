using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream : AbstractAnvilBase
    {
        public Type Type { get; }

        internal AccessController AccessController { get; }

        private readonly string m_TypeString;

        protected AbstractDataStream()
        {
            Type = GetType();
            
            //TODO: #112 (anvil-csharp-core) Extract to Anvil-CSharp Util method -Used in AbstractJobConfig as well
            m_TypeString = Type.IsGenericType
                ? $"{Type.Name[..^2]}<{Type.GenericTypeArguments[0].Name}>"
                : Type.Name;
            
            AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return m_TypeString;
        }
    }
}
