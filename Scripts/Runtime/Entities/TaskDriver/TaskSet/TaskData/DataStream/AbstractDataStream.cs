namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStream
    {
        internal ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }

        public abstract uint GetActiveID();
    }
}
