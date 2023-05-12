namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataStream : AbstractDataStream
    {
        private const string UNIQUE_CONTEXT_IDENTIFIER = "CANCEL_PROGRESS";
        
        public override DataTargetID DataTargetID
        {
            get => ActiveLookupData.WorldUniqueID;
        }

        public override IDataSource DataSource
        {
            get => m_DataSource;
        }

        private readonly CancelProgressDataSource m_DataSource;

        public ActiveLookupData<EntityProxyInstanceID> ActiveLookupData { get; }

        public CancelProgressDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            m_DataSource = taskDriverManagementSystem.GetCancelProgressDataSource();

            ActiveLookupData = m_DataSource.CreateActiveLookupData(TaskSetOwner, UNIQUE_CONTEXT_IDENTIFIER);
        }
    }
}
