using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
using Unity.Collections;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream : AbstractAnvilBase
    {
        public static readonly BulkScheduleDelegate<AbstractDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);

        public readonly AbstractTaskDriverWork OwningTaskDriverWork;

        public Type Type { get; }

        public AccessController AccessController { get; }

#if DEBUG
        protected DataStreamProfilingInfo Debug_ProfilingInfo { get; }
        protected internal abstract long Debug_PendingBytesPerInstance { get; }
        protected internal abstract long Debug_LiveBytesPerInstance { get; }
        protected internal abstract Type Debug_InstanceType { get; }
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
        protected FixedString128Bytes Debug_DebugString { get; }
#endif

        protected AbstractDataStream(AbstractTaskDriverWork owningTaskDriverWork)
        {
            Type = GetType();
            AccessController = new AccessController();
            OwningTaskDriverWork = owningTaskDriverWork;


#if DEBUG
            Debug_ProfilingInfo = new DataStreamProfilingInfo(this);
            DataStreamProfilingUtil.RegisterProfilingStats(Debug_ProfilingInfo);
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

#if DEBUG
            Debug_ProfilingInfo.Dispose();
#endif

            base.DisposeSelf();
        }

        protected virtual void DisposeDataStream()
        {
        }

        public sealed override string ToString()
        {
            return $"{Type.GetReadableName()}, {OwningTaskDriverWork}";
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************

        protected abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);

#if DEBUG
        public void PopulateProfiler()
        {
            DataStreamProfilingUtil.Debug_InstancesLiveCapacity.Value += Debug_ProfilingInfo.ProfilingDetails.LiveCapacity;
            DataStreamProfilingUtil.Debug_InstancesLiveCount.Value += Debug_ProfilingInfo.ProfilingDetails.LiveInstances;
            DataStreamProfilingUtil.Debug_InstancesPendingCapacity.Value += Debug_ProfilingInfo.ProfilingDetails.PendingCapacity;
            DataStreamProfilingUtil.UpdateStatsByType(Debug_ProfilingInfo);
        }
#endif
    }
}
