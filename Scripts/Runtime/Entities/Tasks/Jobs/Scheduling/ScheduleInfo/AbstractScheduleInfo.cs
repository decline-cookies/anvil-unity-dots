using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractScheduleInfo
    {
        public abstract JobHandle CallScheduleFunction(JobHandle dependsOn);

        public string ScheduleJobFunctionDebugInfo { get; }

        protected AbstractScheduleInfo(MemberInfo scheduleJobFunctionMethodInfo)
        {
            ScheduleJobFunctionDebugInfo = $"{scheduleJobFunctionMethodInfo.DeclaringType?.Name}.{scheduleJobFunctionMethodInfo.Name}";
        }
    }
}
