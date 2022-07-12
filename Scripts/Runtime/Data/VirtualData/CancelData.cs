using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: DISCUSS - Further Abstraction or just like so?
    public class CancelData : AbstractAnvilBase
    {
        internal static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<VDContextID>();

        internal UnsafeTypedStream<VDContextID> Pending;
        internal DeferredNativeArray<VDContextID> IterationTarget;
        internal UnsafeParallelHashMap<VDContextID, bool> Lookup;
        
        internal AccessController AccessController { get; }
        internal Type Type { get; }

        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => IterationTarget.ScheduleInfo;
        }

        internal CancelData(AbstractTaskDriver taskDriver) : this()
        {
            Type = taskDriver.GetType();
        }

        internal CancelData(AbstractTaskDriverSystem taskDriverSystem) : this()
        {
            Type = taskDriverSystem.GetType();
        }

        private CancelData()
        {
            Pending = new UnsafeTypedStream<VDContextID>(Allocator.Persistent,
                                                         Allocator.TempJob);
            IterationTarget = new DeferredNativeArray<VDContextID>(Allocator.Persistent,
                                                                   Allocator.TempJob);
            Lookup = new UnsafeParallelHashMap<VDContextID, bool>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
            
            AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            Pending.Dispose();
            IterationTarget.Dispose();
            Lookup.Dispose();
            
            AccessController.Dispose();

            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
        
        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal VDLookupReader<bool> CreateVDLookupReader()
        {
            return new VDLookupReader<bool>(Lookup);
        }

        internal VDCancelWriter CreateVDCancelWriter(int context)
        {
            return new VDCancelWriter(Pending.AsWriter(), context);
        }

        internal VDReader<VDContextID> CreateVDReader()
        {
            return new VDReader<VDContextID>(IterationTarget.AsDeferredJobArray());
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateCancelLookupJob consolidateCancelLookupJob = new ConsolidateCancelLookupJob(Pending,
                                                                                                   IterationTarget,
                                                                                                   Lookup);
            JobHandle consolidateHandle = consolidateCancelLookupJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelLookupJob : IJob
        {
            private UnsafeTypedStream<VDContextID> m_Pending;
            private DeferredNativeArray<VDContextID> m_Iteration;
            private UnsafeParallelHashMap<VDContextID, bool> m_Lookup;

            public ConsolidateCancelLookupJob(UnsafeTypedStream<VDContextID> pending,
                                              DeferredNativeArray<VDContextID> iteration,
                                              UnsafeParallelHashMap<VDContextID, bool> lookup)
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                //Clear previously consolidated 
                m_Lookup.Clear();
                m_Iteration.Clear();

                //Get the new count
                int pendingCount = m_Pending.Count();
                
                //Allocate memory for array based on count
                NativeArray<VDContextID> iterationArray = m_Iteration.DeferredCreate(pendingCount);

                //Fast blit
                m_Pending.CopyTo(ref iterationArray);

                //Populate the lookup
                for (int i = 0; i < pendingCount; ++i)
                {
                    VDContextID contextID = iterationArray[i];
                    m_Lookup.TryAdd(contextID, true);
                }

                //Clear pending for next frame
                m_Pending.Clear();
            }
        }
    }
}
