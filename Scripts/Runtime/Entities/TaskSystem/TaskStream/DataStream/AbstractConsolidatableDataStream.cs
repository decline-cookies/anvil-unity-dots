using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractConsolidatableDataStream : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractConsolidatableDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractConsolidatableDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);

        internal AbstractTaskDriver OwningTaskDriver { get; }
        internal AbstractTaskSystem OwningTaskSystem { get; }
        
        public Type Type { get; }

        internal AccessController AccessController { get; }
        
        //TODO: Hide this stuff when collections checks are disabled
        protected ProfilerMarker Debug_ProfilerMarker { get; }
        protected FixedString128Bytes Debug_DebugString { get; }

        protected AbstractConsolidatableDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            Type = GetType();
            AccessController = new AccessController();
            OwningTaskDriver = taskDriver;
            OwningTaskSystem = taskSystem;
            
            Debug_DebugString = new FixedString128Bytes(ToString());
            Debug_ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, Debug_DebugString.Value, MarkerFlags.Script);
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

        public sealed override string ToString()
        {
            return $"{Type.GetReadableName()}, {TaskDebugUtil.GetLocationName(OwningTaskSystem, OwningTaskDriver)}";
        }
    }
}
