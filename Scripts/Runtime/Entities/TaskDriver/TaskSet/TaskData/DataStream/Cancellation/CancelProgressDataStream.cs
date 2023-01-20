namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataStream : AbstractDataStream
    {
        private readonly CancelProgressDataSource m_DataSource;
        
        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public CancelProgressDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelProgressDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner);
        }

        public override uint GetActiveID()
        {
            return ActiveLookupData.ID;
        }
    }
}
