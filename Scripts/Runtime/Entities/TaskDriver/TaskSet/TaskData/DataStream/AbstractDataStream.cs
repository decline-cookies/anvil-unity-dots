namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStream
    {
        public abstract DataTargetID DataTargetID { get; }
        public abstract IDataSource DataSource { get; }
        public ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }
    }
}
