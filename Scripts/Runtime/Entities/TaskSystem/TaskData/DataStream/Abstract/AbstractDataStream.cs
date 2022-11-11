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

        public readonly AbstractTaskDriver OwningTaskDriver;
        public readonly AbstractTaskSystem OwningTaskSystem;

        public Type Type { get; }

        public AccessController AccessController { get; }

#if DEBUG
        protected ProfilingStats Debug_ProfilingStats { get; }
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
        protected FixedString128Bytes Debug_DebugString { get; }
#endif

        protected AbstractDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            Type = GetType();
            AccessController = new AccessController();
            OwningTaskDriver = taskDriver;
            OwningTaskSystem = taskSystem;


#if DEBUG
            Debug_ProfilingStats = new ProfilingStats(this);
            DataStreamProfilingUtil.RegisterProfilingStats(Debug_ProfilingStats);
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
            Debug_ProfilingStats.Dispose();
#endif
            
            base.DisposeSelf();
        }

        protected virtual void DisposeDataStream()
        {
        }
        
        public sealed override string ToString()
        {
            return $"{Type.GetReadableName()}, {TaskDebugUtil.GetLocationName(OwningTaskSystem, OwningTaskDriver)}";
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

        public void PopulateProfiler()
        {
            DataStreamProfilingUtil.Debug_InstancesLiveCapacity.Value += Debug_ProfilingStats.ProfilingInfo.LiveCapacity;
            DataStreamProfilingUtil.Debug_InstancesLiveCount.Value += Debug_ProfilingStats.ProfilingInfo.LiveInstances;
            DataStreamProfilingUtil.Debug_InstancesPendingCapacity.Value += Debug_ProfilingStats.ProfilingInfo.PendingCapacity;
            DataStreamProfilingUtil.UpdateStatsByType(Debug_ProfilingStats);
        }
    }
}
