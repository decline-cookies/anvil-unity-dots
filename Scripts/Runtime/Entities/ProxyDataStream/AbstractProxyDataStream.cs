using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractProxyDataStream : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractProxyDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractProxyDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);

        public Type Type { get; }

        internal AccessController AccessController { get; }

        private readonly string m_TypeString;

        protected AbstractProxyDataStream()
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

        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
        internal abstract unsafe void* GetWriterPointer();
    }
}
