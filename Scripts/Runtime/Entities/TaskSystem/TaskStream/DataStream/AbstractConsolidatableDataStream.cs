using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractConsolidatableDataStream : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractConsolidatableDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractConsolidatableDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);

        public Type Type { get; }

        internal AccessController AccessController { get; }

        protected AbstractConsolidatableDataStream()
        {
            Type = GetType();
            AccessController = new AccessController();
        }

        protected sealed override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            DisposeDataStream();
            AccessController.Dispose();
            base.DisposeSelf();
        }
        
        protected virtual void DisposeDataStream()
        {
        }

        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);

        public override string ToString()
        {
            return Type.GetReadableName();
        }
    }
}
