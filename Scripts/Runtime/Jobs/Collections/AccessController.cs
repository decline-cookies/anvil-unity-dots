using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A utility class that handles managing access for reading and writing so that jobs can be scheduled easily.
    /// </summary>
    /// <remarks>
    /// This can be a little bit complicated to wrap your head around so here is an example
    ///
    /// ----- EXTERNAL WRITE PHASE -----
    /// - Multiple different systems want to write to the collection.
    /// - They schedule their writing jobs using the <see cref="WriteAccessDependency"/> handle.
    /// - This means that all those jobs can start at the same time.
    /// - All of those jobs use <see cref="AddJobHandleForParallelWriting"/> to let the <see cref="AccessController"/> know that there is writing going on. We cannot read until this is done.
    ///
    /// ----- INTERNAL WRITE PHASE -----
    /// - A managing system now needs to do some work where it reads from and writes to the collection.
    /// - It schedules it's job to do that using the <see cref="ReadWriteAccessDependency"/> handle.
    /// - This means that it can do it's work once all the previous external writers have completed.
    /// - This job then uses <see cref="AddJobHandleForReadWriting"/> to the let the <see cref="AccessController"/> know that there is reading and writing going on that cannot be interrupted by external parallel writes.
    ///
    /// ----- EXTERNAL READ PHASE -----
    /// - Multiple different systems want to read from the collection.
    /// - They schedule their reading jobs using the <see cref="ReadAccessDependency"/> handle.
    /// - This means that all those jobs can start at the same time.
    /// - All of those jobs use <see cref="AddJobHandleForParallelReading"/> to let the <see cref="AccessController"/> know that there is reading going on. We cannot write until this is done.
    ///
    /// ----- CLEAN UP PHASE -----
    /// - The collection used above needs to be disposed but we need to ensure all reading and writing are complete.
    /// - The collection disposes using the <see cref="ParallelReadAccessDependency"/> handle.
    /// - This means that all reading from the collection has been completed. It is safe to dispose as no one is using it anymore.
    /// </remarks>
    public class AccessController
    {

        /// <summary>
        /// A <see cref="JobHandle"/> to wait upon before being allowed to read and write in the same job.
        /// </summary>
        public JobHandle ReadWriteAccessDependency
        {
            get => ParallelWriteAccessDependency;
        }
        
        
        /// <summary>
        /// A <see cref="JobHandle"/> to wait upon before being allowed to write.
        /// </summary>
        public JobHandle WriteAccessDependency
        {
            get;
            private set;
        }
        
        /// <summary>
        /// A <see cref="JobHandle"/> to wait upon before being allowed to write in parallel.
        /// </summary>
        public JobHandle ParallelWriteAccessDependency
        {
            get;
            private set;
        }
        
        /// <summary>
        /// A <see cref="JobHandle"/> to wait upon before being allowed to read.
        /// </summary>
        public JobHandle ReadAccessDependency
        {
            get;
            private set;
        }
        
        /// <summary>
        /// A <see cref="JobHandle"/> to wait upon before being allowed to read in parallel.
        /// </summary>
        public JobHandle ParallelReadAccessDependency
        {
            get;
            private set;
        }

        /// <summary>
        /// Ensures that the <see cref="AccessController"/> is aware of a job that is reading and writing.
        /// </summary>
        /// <param name="jobThatReadsAndWrites">The job that reads and writes.</param>
        public void AddJobHandleForReadWriting(JobHandle jobThatReadsAndWrites)
        {
            AddJobHandleForWriting(jobThatReadsAndWrites);
        }
        
        /// <summary>
        /// Ensures that the <see cref="AccessController"/> is aware of a job that is writing.
        /// </summary>
        /// <param name="jobThatWrites">The job that writes.</param>
        public void AddJobHandleForWriting(JobHandle jobThatWrites)
        {
            //No one else can write until this job is done
            WriteAccessDependency = JobHandle.CombineDependencies(WriteAccessDependency, jobThatWrites);
            //No one else can do anything until this job is done writing
            ParallelWriteAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, jobThatWrites);
            ReadAccessDependency = JobHandle.CombineDependencies(ReadAccessDependency, jobThatWrites);
            ParallelReadAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, jobThatWrites);
        }
        
        /// <summary>
        /// Ensures that the <see cref="AccessController"/> is aware of a job that is writing in parallel.
        /// </summary>
        /// <param name="jobThatWritesInParallel">The job that writes in parallel.</param>
        public void AddJobHandleForParallelWriting(JobHandle jobThatWritesInParallel)
        {
            ParallelWriteAccessDependency = JobHandle.CombineDependencies(ParallelWriteAccessDependency, WriteAccessDependency, jobThatWritesInParallel);
            //No one else can read until all writing is complete
            ReadAccessDependency = JobHandle.CombineDependencies(ReadAccessDependency, jobThatWritesInParallel);
            ParallelReadAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, jobThatWritesInParallel);
        }
        
        /// <summary>
        /// Ensures that the <see cref="AccessController"/> is aware of a job that is reading.
        /// </summary>
        /// <param name="jobThatReads">The job that reads.</param>
        public void AddJobHandleForReading(JobHandle jobThatReads)
        {
            ReadAccessDependency = JobHandle.CombineDependencies(ReadAccessDependency, jobThatReads);
            //No one else can write until we're done reading
            WriteAccessDependency = JobHandle.CombineDependencies(WriteAccessDependency, jobThatReads);
            ParallelWriteAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, jobThatReads);
        }

        /// <summary>
        /// Ensures that the <see cref="AccessController"/> is aware of a job that is reading in parallel.
        /// </summary>
        /// <param name="jobThatReadsInParallel">The job that reads in parallel.</param>
        public void AddJobHandleForParallelReading(JobHandle jobThatReadsInParallel)
        {
            ParallelReadAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, ReadAccessDependency, jobThatReadsInParallel);
            //No one else can write until we're done reading
            WriteAccessDependency = JobHandle.CombineDependencies(WriteAccessDependency, jobThatReadsInParallel);
            ParallelWriteAccessDependency = JobHandle.CombineDependencies(ParallelReadAccessDependency, jobThatReadsInParallel);
        }
    }
}
