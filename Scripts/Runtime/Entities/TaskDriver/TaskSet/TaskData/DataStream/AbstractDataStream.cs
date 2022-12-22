namespace Anvil.Unity.DOTS.Entities.Tasks
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
