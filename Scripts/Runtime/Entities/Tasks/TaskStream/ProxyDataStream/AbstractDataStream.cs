using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Think on this name some more. Is is actually an AbstractDataStreamProxy? https://github.com/decline-cookies/anvil-unity-dots/pull/59#discussion_r977766979
    public abstract class AbstractDataStream : AbstractAnvilBase
    {
        public Type Type { get; }

        internal AccessController AccessController { get; }

        private readonly string m_TypeString;

        protected AbstractDataStream()
        {
            Type = GetType();
            
            //TODO: Extract to Anvil-CSharp Util method -Used in AbstractJobConfig as well
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
