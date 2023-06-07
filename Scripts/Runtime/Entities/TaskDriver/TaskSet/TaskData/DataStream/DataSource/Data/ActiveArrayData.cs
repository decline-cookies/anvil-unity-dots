using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class ActiveArrayData<T> : AbstractData
        where T : unmanaged, IEquatable<T>
    {
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<T>() / 8.0f);

        private DeferredNativeArray<T> m_Active;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        //TODO: #136 - Clean up food for thought - https://github.com/decline-cookies/anvil-unity-dots/pull/142#discussion_r1082756502
        public NativeArray<T> DeferredJobArray
        {
            get => m_Active.AsDeferredJobArray();
        }

        public DeferredNativeArray<T> Active
        {
            get => m_Active;
        }

        public NativeArray<T> CurrentArray
        {
            get => m_Active.AsArray();
        }

        public sealed override bool IsDataInvalidated
        {
            get => base.IsDataInvalidated && m_Active.Length > 0;
        }

        public ActiveArrayData(
            IDataOwner dataOwner,
            CancelRequestBehaviour cancelRequestBehaviour,
            AbstractData pendingCancelActiveData,
            string uniqueContextIdentifier)
            : base(
                dataOwner,
                cancelRequestBehaviour,
                pendingCancelActiveData,
                uniqueContextIdentifier)
        {
            m_Active = new DeferredNativeArray<T>(Allocator.Persistent);
            m_Active.SetCapacity(INITIAL_SIZE);

            ScheduleInfo = m_Active.ScheduleInfo;
        }

        protected sealed override void DisposeData()
        {
            m_Active.Dispose();
        }
    }
}