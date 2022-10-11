namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskDebugUtil
    {
        public static string GetLocationName(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? $"{taskSystem.GetType().Name}"
                : $"{taskDriver.GetType().Name} as part of the {taskSystem.GetType().Name} system";
        }
    }
}
