using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelRequestsDataStream : AbstractDataStream
    {
        private readonly CancelRequestsDataSource m_DataSource;
        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public override uint ActiveID
        {
            get => ActiveLookupData.ID;
        }

        public CancelRequestsDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelRequestsDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_DataSource.AcquirePendingAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_DataSource.ReleasePendingAsync(dependsOn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquirePending(AccessType accessType)
        {
            m_DataSource.AcquirePending(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePending()
        {
            m_DataSource.ReleasePending();
        }

        public CancelRequestsWriter CreateCancelRequestsWriter()
        {
            return new CancelRequestsWriter(m_DataSource.PendingWriter, TaskSetOwner.TaskSet.CancelRequestsContexts);
        }
    }
}
