using Anvil.Unity.DOTS.Systems;
using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// An <see cref="IJob"/> job that will take up CPU time by calculating the Nth prime number passed in.
    /// Useful for artificially increasing the time a job takes to help with debugging scheduling or profiling.
    /// <seealso cref="DebugUtil.FindPrimeNumber"/>
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct BusyWorkJob : IJobFor
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

        public void Execute(int index)
        {
            DebugUtil.FindPrimeNumber(m_NthPrimeNumberToFind);
        }
    }
}
