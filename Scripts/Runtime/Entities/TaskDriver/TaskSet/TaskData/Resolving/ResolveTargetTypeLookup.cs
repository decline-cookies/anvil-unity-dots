using System;
using System.Collections.Generic;
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
                //TODO: Debug ensure not already there
                m_ResolveTargetWriteDataByID.Add(targetID, resolveTargetWriteData);
            }
        }

        public unsafe void Resolve<TResolveTargetType>(uint taskSetOwnerID, int laneIndex, ref TResolveTargetType resolvedInstance)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint typeID = ResolveTargetUtil.GetResolveTargetID<TResolveTargetType>();
            ResolveTargetID targetID = new ResolveTargetID(typeID, taskSetOwnerID);
            //TODO: Debug ensure it exists
            ResolveTargetWriteData resolveTargetWriteData = m_ResolveTargetWriteDataByID[targetID];
            void* writerPtr = (void*)resolveTargetWriteData.PendingWriterPointerAddress;
            DataStreamPendingWriter<TResolveTargetType> writer = new DataStreamPendingWriter<TResolveTargetType>(writerPtr, taskSetOwnerID, resolveTargetWriteData.ActiveID, laneIndex);
            writer.Add(ref resolvedInstance);
        }
    }
}
