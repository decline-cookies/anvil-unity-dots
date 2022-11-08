using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
using Unity.Profiling.LowLevel;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using Unity.Collections;
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

#if DEBUG
        protected ProfilerMarker Debug_ProfilerMarker { get; }
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
        protected FixedString128Bytes Debug_DebugString { get; }
#endif

        protected AbstractDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            Type = GetType();
            AccessController = new AccessController();
            m_OwningTaskDriver = taskDriver;
            m_OwningTaskSystem = taskSystem;


#if DEBUG
            Debug_ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, ToString(), MarkerFlags.Script);
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            Debug_DebugString = new FixedString128Bytes(ToString());
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
