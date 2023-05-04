namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStream
    {
        public abstract DataTargetID DataTargetID { get; }
        internal ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }
    }
}
