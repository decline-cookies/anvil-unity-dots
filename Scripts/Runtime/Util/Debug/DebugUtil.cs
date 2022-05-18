namespace Anvil.Unity.DOTS.Systems
{
    /// <summary>
    /// Useful helpers to aid in debugging
    /// </summary>
    public static class DebugUtil
    {
        /// <summary>
        /// Calculates the Nth prime number that is passed in
        /// </summary>
        /// <param name="nthPrimeNumberToFind">The Nth prime number to find.
        /// Ex.
        /// 1 = the first prime number or 2,
        /// 100 = the 100th prime number or 541
        /// </param>
        /// <returns>The Nth prime number</returns>
        public static long FindPrimeNumber(int nthPrimeNumberToFind)
        {
            int count = 0;
            long a = 2;
            while (count < nthPrimeNumberToFind)
            {
                long b = 2;
                int prime = 1;
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }

                    b++;
                }

                if (prime > 0)
                {
                    count++;
                }

                a++;
            }

            return (--a);
        }
    }
}
