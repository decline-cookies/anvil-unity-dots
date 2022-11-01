using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractCancelFlow : AbstractAnvilBase
    {
        internal CancelRequestDataStream RequestDataStream { get; }
        internal CancelProgressDataStream ProgressDataStream { get; }
        internal CancelCompleteDataStream CompleteDataStream { get; }


        private NativeArray<byte> m_Contexts;

        //Request data
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_RequestWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_RequestLaneWriters;

        //Progress data
        private NativeArray<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_ProgressLookups;

        //Complete data
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_CompleteWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_CompleteLaneWriters;


        protected AbstractCancelFlow()
        {
            /// Wander (1) gets a RequestCancel for Entity 3
            ///     Timer Task Driver (1) will get a RequestCancel For Entity 3
            ///         Timer System needs to get Entity 3 (1)
            ///     Pathfind Task Driver (1) will get a RequestCancel for Entity 3
            ///         Pathfind System needs to get Entity 3 (1)
            ///     Follow Path Task Driver (1) will get a RequestCancel for Entity 3
            ///         Follow Path System needs to get Entity 3 (1)
            ///         Move To Task Driver (1) will get a RequestCancel for Entity 3
            ///             Move To System needs to get Entity 3 (1)
            ///     Wander System needs to get Entity 3 (1)
            ///


            CompleteDataStream = new CancelCompleteDataStream();
            ProgressDataStream = new CancelProgressDataStream(CompleteDataStream);
            RequestDataStream = new CancelRequestDataStream(ProgressDataStream);
        }

        protected override void DisposeSelf()
        {
            RequestDataStream.Dispose();
            ProgressDataStream.Dispose();
            CompleteDataStream.Dispose();

            //TODO: Dispose native arrays


            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal CancelRequestsWriter CreateCancelRequestsWriter()
        {
            return new CancelRequestsWriter(m_RequestWriters,
                                            m_RequestLaneWriters,
                                            m_Contexts);
        }

        public void Harden()
        {
            HardenRequests();
            HardenCompletes();
        }

        private void HardenRequests()
        {
            List<CancelRequestDataStream> cancelRequests = new List<CancelRequestDataStream>();
            List<byte> cancelContexts = new List<byte>();
            AddRequestDataTo(cancelRequests, cancelContexts);

            m_RequestWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer>(cancelRequests.Count, Allocator.Persistent);
            m_RequestLaneWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter>(cancelRequests.Count, Allocator.Persistent);
            m_Contexts = new NativeArray<byte>(cancelRequests.Count, Allocator.Persistent);

            for (int i = 0; i < m_RequestWriters.Length; ++i)
            {
                m_RequestWriters[i] = cancelRequests[i].Pending.AsWriter();
                m_Contexts[i] = cancelContexts[i];
            }
        }

        protected abstract void AddRequestDataTo(List<CancelRequestDataStream> cancelRequests,
                                                 List<byte> contexts);


        private void HardenCompletes()
        {
            List<CancelCompleteDataStream> cancelCompletes = new List<CancelCompleteDataStream>();
            AddCompleteDataTo(cancelCompletes);
        }

        private void AddCompleteDataTo(List<CancelCompleteDataStream> cancelCompletes)
        {
            //TODO: necessary?
        }
    }
}
