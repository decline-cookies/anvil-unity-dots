using Anvil.Unity.DOTS.Util;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A collection of extension methods for <see cref="JobHandle"/>.
    /// </summary>
    public static class JobHandleExtension
    {
        /// <summary>
        /// Checks if a <see cref="JobHandle"/> depends on another to be complete before starting.
        /// </summary>
        /// <param name="job">The <see cref="JobHandle"/> that might need to wait to start.</param>
        /// <param name="candidateJob">The <see cref="JobHandle"/> to check if it needs to complete or not.</param>
        /// <returns>
        /// True if the <paramref name="candidateJob"/> must complete before <paramref name="job"/> can start.
        /// False if not.
        /// </returns>
        public static bool DependsOn(this JobHandle job, JobHandle candidateJob)
        {
            return JobHandle.CheckFenceIsDependencyOrDidSyncFence(candidateJob, job);
        }

        /// <summary>
        /// Checks if a <see cref="JobHandle"/> is further down the chain from another.
        /// </summary>
        /// <param name="job">The <see cref="JobHandle"/> that might need to be completed before the <paramref name="candidateJob"/> can start.</param>
        /// <param name="candidateJob">The <see cref="JobHandle"/> to check if it will wait to start.</param>
        /// <returns>
        /// True if the <paramref name="candidateJob"/> will wait until the <see cref="job"/> is complete before starting.
        /// False if not.
        /// </returns>
        public static bool IsDependencyOf(this JobHandle job, JobHandle candidateJob)
        {
            return JobHandle.CheckFenceIsDependencyOrDidSyncFence(job, candidateJob);
        }

        /// <summary>
        /// Determines whether two <see cref="JobHandle"/> instances are equal without boxing.
        /// </summary>
        /// <param name="job1">The first <see cref="JobHandle"/> compare.</param>
        /// <param name="job2">The second <see cref="JobHandle"/> compare.</param>
        /// <returns>True if the <see cref="JobHandle"/>s are the same.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool Equals_NoBox(this in JobHandle job1, in JobHandle job2)
        {
            return UnsafeUtil.Equals_NoBox(in job1, in job2);
        }
    }
}