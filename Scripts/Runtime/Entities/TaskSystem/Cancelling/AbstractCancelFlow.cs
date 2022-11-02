using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractCancelFlow : AbstractAnvilBase
    {
        // internal static readonly BulkScheduleDelegate<AbstractCancelFlow> CHECK_CANCEL_PROGRESS_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractCancelFlow>(nameof(ScheduleCheckCancelProgressJob), BindingFlags.Instance | BindingFlags.NonPublic);

        
        internal CancelRequestDataStream RequestDataStream { get; }
        internal CancelCompleteDataStream CompleteDataStream { get; }


        //Request data
        private NativeArray<byte> m_RequestContexts;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_RequestWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_RequestLaneWriters;

        //Progress data
        private readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_ProgressLookup;

        //Complete data
        private AbstractCancelFlow m_ParentCancelFlow;
        private byte m_ParentContext;
        private NativeArray<JobHandle> m_Dependencies;


        protected AbstractCancelFlow ParentCancelFlow
        {
            get => m_ParentCancelFlow;
            set
            {
                Debug_EnsureParentCancelFlowNotSet();
                m_ParentCancelFlow = value;
            }
        }


        protected AbstractCancelFlow()
        {
            m_ProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                    Allocator.Persistent));

            CompleteDataStream = new CancelCompleteDataStream();
            RequestDataStream = new CancelRequestDataStream(this);
        }

        protected override void DisposeSelf()
        {
            m_ProgressLookup.Dispose();
            RequestDataStream.Dispose();
            CompleteDataStream.Dispose();

            //TODO: Dispose native arrays


            base.DisposeSelf();
        }

        internal JobHandle AcquireProgressLookup(AccessType accessType, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup)
        {
            return m_ProgressLookup.AcquireAsync(accessType, out progressLookup);
        }

        internal void ReleaseProgressLookup(JobHandle releaseAccessDependency)
        {
            m_ProgressLookup.ReleaseAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal CancelRequestsWriter CreateCancelRequestsWriter()
        {
            return new CancelRequestsWriter(m_RequestWriters,
                                            m_RequestLaneWriters,
                                            m_RequestContexts);
        }

        public void Harden()
        {
            List<CancelRequestDataStream> cancelRequests = new List<CancelRequestDataStream>();
            List<byte> cancelContexts = new List<byte>();
            BuildRelationshipData(this, cancelRequests, cancelContexts);

            m_RequestWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer>(cancelRequests.Count, Allocator.Persistent);
            m_RequestLaneWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter>(cancelRequests.Count, Allocator.Persistent);
            m_RequestContexts = new NativeArray<byte>(cancelRequests.Count, Allocator.Persistent);

            for (int i = 0; i < m_RequestWriters.Length; ++i)
            {
                m_RequestWriters[i] = cancelRequests[i].Pending.AsWriter();
                m_RequestContexts[i] = cancelContexts[i];
            }
        }

        internal abstract void BuildRelationshipData(AbstractCancelFlow parentCancelFlow,
                                                     List<CancelRequestDataStream> cancelRequests,
                                                     List<byte> contexts);


        public JobHandle ScheduleCheckCancelProgressJob(JobHandle dependsOn)
        {
            UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup = default;
            
            m_Dependencies[0] = dependsOn;
            m_Dependencies[1] = AcquireProgressLookup(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup);
            m_Dependencies[2] = CompleteDataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            m_Dependencies[3] = m_ParentCancelFlow?.AcquireProgressLookup(AccessType.ExclusiveWrite, out parentProgressLookup) ?? default;
            
            dependsOn = JobHandle.CombineDependencies(m_Dependencies);

            CheckCancelProgressJob checkCancelProgressJob = new CheckCancelProgressJob(parentProgressLookup,
                                                                                       m_ParentCancelFlow?.m_ParentContext ?? 0,
                                                                                       progressLookup,
                                                                                       CompleteDataStream.Pending.AsWriter());

            dependsOn = checkCancelProgressJob.Schedule(dependsOn);
            
            ReleaseProgressLookup(dependsOn);
            CompleteDataStream.AccessController.ReleaseAsync(dependsOn);
            m_ParentCancelFlow?.ReleaseProgressLookup(dependsOn);

            return dependsOn;
        }


        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct CheckCancelProgressJob : IJob
        {
            [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

            [ReadOnly] private readonly byte m_ParentContext;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;

            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ParentProgressLookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;

            public CheckCancelProgressJob(UnsafeParallelHashMap<EntityProxyInstanceID, bool> parentProgressLookup,
                                          byte parentContext,
                                          UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                          UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter) : this()
            {
                m_ParentProgressLookup = parentProgressLookup;
                m_ParentContext = parentContext;
                m_ProgressLookup = progressLookup;
                m_CompleteWriter = completeWriter;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                //We want to check and see if any of our cancels are complete
                NativeArray<EntityProxyInstanceID> ids = m_ProgressLookup.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < ids.Length; ++i)
                {
                    EntityProxyInstanceID id = ids[i];
                    EntityProxyInstanceID parentID = new EntityProxyInstanceID(id.Entity, m_ParentContext);
                    //If we're still processing...
                    if (m_ProgressLookup[id] == true)
                    {
                        //Only if we have a parent...
                        if (m_ParentProgressLookup.IsCreated)
                        {
                            //Tell our parent to keep processing too
                            m_ParentProgressLookup[parentID] = true;
                        }

                        //Flip us back to not processing. A CancelJob will switch this if we still need to process
                        m_ProgressLookup[id] = false;
                        return;
                    }
                    //If we're not processing, then:
                    // - All Cancel Jobs are complete 
                    // OR
                    // - There never were any Cancel Jobs to begin with
                    // OR
                    // - There wasn't any data for this id that was requested to cancel.
                    else
                    {
                        //We can remove ourselves from the lookup
                        m_ProgressLookup.Remove(id);
                        //We can write to our cancel complete
                        m_CompleteLaneWriter.Write(id);
                    }
                }
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureParentCancelFlowNotSet()
        {
            if (m_ParentCancelFlow != null)
            {
                throw new InvalidOperationException($"Tried to set {nameof(ParentCancelFlow)} but it is already set!");
            }
        }
    }
}
