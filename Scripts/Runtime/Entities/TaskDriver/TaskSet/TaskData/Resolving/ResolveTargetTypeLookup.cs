using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct ResolveTargetTypeLookup : IDisposable
    {
        private UnsafeParallelHashMap<ResolveTargetID, ResolveTargetWriteData> m_ResolveTargetWriteDataByID;

        public ResolveTargetTypeLookup(int count)
        {
            m_ResolveTargetWriteDataByID = new UnsafeParallelHashMap<ResolveTargetID, ResolveTargetWriteData>(count, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (m_ResolveTargetWriteDataByID.IsCreated)
            {
                m_ResolveTargetWriteDataByID.Dispose();
            }
        }

        public void CreateWritersForType(ResolveTargetDefinition targetDefinition, List<AbstractDataStream> dataStreams)
        {
            foreach (AbstractDataStream dataStream in dataStreams)
            {
                ResolveTargetID targetID = new ResolveTargetID(targetDefinition.TypeID, dataStream.TaskSetOwner.ID);
                ResolveTargetWriteData resolveTargetWriteData = new ResolveTargetWriteData(targetDefinition.PendingWriterPointerAddress, dataStream.GetActiveID());
                Debug_EnsureNotPresent(targetID);
                m_ResolveTargetWriteDataByID.Add(targetID, resolveTargetWriteData);
            }
        }

        public unsafe void Resolve<TResolveTargetType>(uint taskSetOwnerID, int laneIndex, ref TResolveTargetType resolvedInstance)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint typeID = ResolveTargetUtil.GetResolveTargetID<TResolveTargetType>();
            ResolveTargetID targetID = new ResolveTargetID(typeID, taskSetOwnerID);
            Debug_EnsurePresent(targetID);
            ResolveTargetWriteData resolveTargetWriteData = m_ResolveTargetWriteDataByID[targetID];
            void* writerPtr = (void*)resolveTargetWriteData.PendingWriterPointerAddress;
            DataStreamPendingWriter<TResolveTargetType> writer = new DataStreamPendingWriter<TResolveTargetType>(writerPtr, taskSetOwnerID, resolveTargetWriteData.ActiveID, laneIndex);
            writer.Add(ref resolvedInstance);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotPresent(ResolveTargetID resolveTargetID)
        {
            if (m_ResolveTargetWriteDataByID.ContainsKey(resolveTargetID))
            {
                throw new InvalidOperationException($"Trying to add {resolveTargetID} but it already exists!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsurePresent(ResolveTargetID resolveTargetID)
        {
            if (!m_ResolveTargetWriteDataByID.ContainsKey(resolveTargetID))
            {
                throw new InvalidOperationException($"Trying to get {resolveTargetID} but it doesn't exist!");
            }
        }
    }
}
