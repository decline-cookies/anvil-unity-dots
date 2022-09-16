using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractProxyDataStream : AbstractAnvilBase,
                                                    IProxyDataStream
    {
        public string DebugString
        {
            get => m_Type.Name;
        }

        internal AccessController AccessController
        {
            get;
        }

        private readonly Type m_Type;

        protected AbstractProxyDataStream()
        {
            m_Type = GetType();
            AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();
            base.DisposeSelf();
        }

        public abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
    }
}
