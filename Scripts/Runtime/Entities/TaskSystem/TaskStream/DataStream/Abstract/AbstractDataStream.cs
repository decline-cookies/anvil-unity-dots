using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
using Unity.Collections;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream : AbstractAnvilBase
    {
        public static readonly BulkScheduleDelegate<AbstractDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly AbstractTaskDriver m_OwningTaskDriver;
        private readonly AbstractTaskSystem m_OwningTaskSystem;

        public Type Type { get; }

        public AccessController AccessController { get; }

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
        protected ProfilerMarker Debug_ProfilerMarker { get; }
        protected FixedString128Bytes Debug_DebugString { get; }
#endif

        protected AbstractDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            Type = GetType();
            AccessController = new AccessController();
            m_OwningTaskDriver = taskDriver;
            m_OwningTaskSystem = taskSystem;

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            Debug_DebugString = new FixedString128Bytes(ToString());
            Debug_ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, Debug_DebugString.Value, MarkerFlags.Script);
#endif
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
            return $"{Type.GetReadableName()}, {TaskDebugUtil.GetLocationName(m_OwningTaskSystem, m_OwningTaskDriver)}";
        }
    }
}
