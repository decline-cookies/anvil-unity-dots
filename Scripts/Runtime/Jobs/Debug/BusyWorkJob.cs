using Anvil.Unity.DOTS.Util;
using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// An <see cref="IJob"/> job that will take up CPU time by calculating the Nth prime number passed in.
    /// Useful for artificially increasing the time a job takes to help with debugging scheduling or profiling.
    /// <seealso cref="DebugUtil.FindPrimeNumber"/>
    /// </summary>
    [BurstCompile]
    public struct BusyWorkJob : IJob
    {
        private readonly int m_NthPrimeNumberToFind;

        /// <summary>
        /// Creates a <see cref="BusyWorkJob"/>
        /// </summary>
        /// <param name="nthPrimeNumberToFind">The Nth prime number to find.
        /// Ex.
        /// 1 = the first prime number or 2,
        /// 100 = the 100th prime number or 541
        /// </param>
        public BusyWorkJob(int nthPrimeNumberToFind)
        {
            m_NthPrimeNumberToFind = nthPrimeNumberToFind;
        }

        public void Execute()
        {
            DebugUtil.FindPrimeNumber(m_NthPrimeNumberToFind);
        }
    }

    /// <summary>
    /// An <see cref="IJobParallelForBatch"/> job that will take up CPU time by calculating the Nth prime number passed
    /// in. Useful for artificially increasing the time a job takes to help with debugging scheduling or profiling.
    /// <seealso cref="DebugUtil.FindPrimeNumber"/>
    /// </summary>
    [BurstCompile]
    public struct BusyWorkBatchJob : IJobParallelForBatch
    {
        private readonly int m_NthPrimeNumberToFind;

        /// <summary>
        /// Creates a <see cref="BusyWorkBatchJob"/>
        /// </summary>
        /// <param name="nthPrimeNumberToFind">The Nth prime number to find.
        /// Ex.
        /// 1 = the first prime number or 2,
        /// 100 = the 100th prime number or 541
        /// </param>
        public BusyWorkBatchJob(int nthPrimeNumberToFind)
        {
            m_NthPrimeNumberToFind = nthPrimeNumberToFind;
        }

        public void Execute(int startIndex, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                DebugUtil.FindPrimeNumber(m_NthPrimeNumberToFind);
            }
        }
    }
}
