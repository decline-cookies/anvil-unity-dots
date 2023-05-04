using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelRequestsDataStream : AbstractDataStream,
                                              IDriverCancelRequestDataStream,
                                              ISystemCancelRequestDataStream
    {
        private readonly CancelRequestsDataSource m_DataSource;
        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public override DataTargetID DataTargetID
        {
            get => ActiveLookupData.DataTargetID;
        }

        public override IDataSource DataSource
        {
            get => m_DataSource;
        }

        public CancelRequestsDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelRequestsDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner, "CANCEL_REQUEST");
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

        public JobHandle AcquireCancelRequestsWriterAsync(out CancelRequestsWriter cancelRequestsWriter)
        {
            JobHandle dependsOn = AcquirePendingAsync(AccessType.SharedWrite);
            cancelRequestsWriter = CreateCancelRequestsWriter();
            return dependsOn;
        }

        public void ReleaseCancelRequestsWriterAsync(JobHandle dependsOn)
        {
            ReleasePendingAsync(dependsOn);
        }

        public CancelRequestsWriter AcquireCancelRequestsWriter()
        {
            AcquirePending(AccessType.SharedWrite);
            return CreateCancelRequestsWriter();
        }

        public void ReleaseCancelRequestsWriter()
        {
            ReleasePending();
        }
    }
}
