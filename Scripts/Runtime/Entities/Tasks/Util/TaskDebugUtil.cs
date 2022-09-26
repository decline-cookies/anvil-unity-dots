namespace Anvil.Unity.DOTS.Entities
{
    internal static class TaskDebugUtil
    {
        public static string GetLocationName(ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? $"{taskSystem.GetType().Name}"
                : $"{taskDriver.GetType().Name} as part of the {taskSystem.GetType().Name} system";
        }
    }
}
