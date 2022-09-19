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
        
        public Type Type
        {
            get;
        }

        internal AccessController AccessController
        {
            get;
        }

        protected AbstractProxyDataStream()
        {
            Type = GetType();
            AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{Type.Name}";
        }

        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
    }
}
