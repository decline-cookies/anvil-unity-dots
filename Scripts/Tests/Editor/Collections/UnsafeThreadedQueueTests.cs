using Anvil.Unity.DOTS.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities.Tests;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Tests
{
    internal class UnsafeThreadedQueueTests : ECSTestsFixture
    {
        private static void ExpectedCount(ref UnsafeThreadedQueue<int> unsafeThreadedQueue, int expected)
        {
            Assert.AreEqual(expected == 0, unsafeThreadedQueue.IsEmpty());
            Assert.AreEqual(expected, unsafeThreadedQueue.Count());
        }
        
        [Test]
        public void CreateAndDestroy([Values(1, 100, 200)] int count)
        {
            UnsafeThreadedQueue<int> unsafeThreadedQueue = new UnsafeThreadedQueue<int>(count, Allocator.Temp);
            
            Assert.IsTrue(unsafeThreadedQueue.IsCreated);
            ExpectedCount(ref unsafeThreadedQueue, 0);

            unsafeThreadedQueue.Dispose();
            Assert.IsFalse(unsafeThreadedQueue.IsCreated);
        }
    }
}

