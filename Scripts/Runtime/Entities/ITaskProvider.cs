namespace Anvil.Unity.DOTS.Entities
{
    public interface ITaskProvider<TTask>
    {
        public TTask CreateTask();
    }
}
