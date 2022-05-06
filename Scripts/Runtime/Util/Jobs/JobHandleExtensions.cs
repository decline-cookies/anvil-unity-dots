using Unity.Jobs;

namespace Anvil.Unity.DOTS.Util
{
    public static class JobHandleExtensions
    {
        //Before we're allowed to go
        public static bool DependsOn(this JobHandle job, JobHandle candidateJob)
        {
            return JobHandle.CheckFenceIsDependencyOrDidSyncFence(candidateJob, job);
        }
        
        public static bool IsDependencyOf(this JobHandle job, JobHandle candidateJob)
        {
            return JobHandle.CheckFenceIsDependencyOrDidSyncFence(job, candidateJob);
        }
    }
}
