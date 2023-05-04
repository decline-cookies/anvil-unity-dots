namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataStream : AbstractDataStream
    {
        public override DataTargetID DataTargetID
        {
            get => ActiveLookupData.DataTargetID;
        }

        private readonly CancelProgressDataSource m_DataSource;

        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public CancelProgressDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelProgressDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner, "CANCEL_PROGRESS");
        }
    }
}
