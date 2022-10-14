namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskDebugUtil
    {
        public static string GetLocationName(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? $"{taskSystem}"
                : $"{taskDriver} as part of the {taskSystem} system";
        }
    }
}
