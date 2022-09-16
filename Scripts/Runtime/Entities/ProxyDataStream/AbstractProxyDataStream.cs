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

        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
    }
}
