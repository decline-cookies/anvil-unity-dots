namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStream
    {
        public abstract uint ActiveID { get; }
        internal ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }
    }
}
