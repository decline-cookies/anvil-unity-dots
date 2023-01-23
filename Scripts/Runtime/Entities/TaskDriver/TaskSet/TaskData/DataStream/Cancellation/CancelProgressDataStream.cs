namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataStream : AbstractDataStream
    {
        public override uint ActiveID
        {
            get => ActiveLookupData.ID;
        }

        private readonly CancelProgressDataSource m_DataSource;
        
        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public CancelProgressDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelProgressDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner);
        }
    }
}
