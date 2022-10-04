using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Scheduling info for a <see cref="AbstractJobConfig"/> for use with a <see cref="JobConfigScheduleDelegates"/>
    /// delegate.
    /// </summary>
    public abstract class AbstractScheduleInfo
    {
        internal abstract JobHandle CallScheduleFunction(JobHandle dependsOn);

        internal string ScheduleJobFunctionDebugInfo { get; }

        protected AbstractScheduleInfo(MemberInfo scheduleJobFunctionMethodInfo)
        {
            ScheduleJobFunctionDebugInfo = $"{scheduleJobFunctionMethodInfo.DeclaringType?.Name}.{scheduleJobFunctionMethodInfo.Name}";
        }
    }
}
