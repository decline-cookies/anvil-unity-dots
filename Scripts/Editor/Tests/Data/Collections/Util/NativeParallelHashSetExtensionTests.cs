using System.Collections.Generic;
using System.Linq;
using Anvil.Unity.DOTS.Data;
using NUnit.Framework;
using Unity.Collections;


namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class NativeParallelHashSetExtensionTests
    {
        // ----- CopyFrom ----- //
        [Test]
        public static void CopyFromTest_NativeParallelHashSet_SmallerSource()
        {
            Assert.That(nameof(CopyFromTest_NativeParallelHashSet_SmallerSource), Does.StartWith(nameof(NativeParallelHashSetExtension.CopyFrom) + "Test"));

            using NativeParallelHashSet<int> sourceSet = new NativeParallelHashSet<int>(1, Allocator.Persistent)
            {
                1
            };
            int sourceSetCount = sourceSet.Count();

            using NativeParallelHashSet<int> destinationSet = new NativeParallelHashSet<int>(3, Allocator.Persistent)
            {
                2, 3, 4
            };
            int originalDestinationCount = destinationSet.Count();

            destinationSet.CopyFrom(sourceSet);

            Assert.That(sourceSet.Count(), Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Capacity, Is.EqualTo(originalDestinationCount));
            Assert.That(destinationSet.Count(), Is.EqualTo(sourceSetCount));
            foreach (int item in sourceSet)
            {
                Assert.That(destinationSet.Contains(item), Is.True);
            }
        }

        [Test]
        public static void CopyFromTest_NativeParallelHashSet_LargerSource()
        {
            Assert.That(nameof(CopyFromTest_NativeParallelHashSet_LargerSource), Does.StartWith(nameof(NativeParallelHashSetExtension.CopyFrom) + "Test"));

            using NativeParallelHashSet<int> sourceSet = new NativeParallelHashSet<int>(3, Allocator.Persistent)
            {
                1, 2, 3
            };
            int sourceSetCount = sourceSet.Count();

            using NativeParallelHashSet<int> destinationSet = new NativeParallelHashSet<int>(1, Allocator.Persistent)
            {
                4
            };
            int originalDestinationCount = destinationSet.Count();

            destinationSet.CopyFrom(sourceSet);

            Assert.That(sourceSet.Count(), Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Capacity, Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Count(), Is.EqualTo(sourceSetCount));
            foreach (int item in sourceSet)
            {
                Assert.That(destinationSet.Contains(item), Is.True);
            }
        }

        [Test]
        public static void CopyFromTest_IEnumerable_SmallerSource()
        {
            Assert.That(nameof(CopyFromTest_IEnumerable_SmallerSource), Does.StartWith(nameof(NativeParallelHashSetExtension.CopyFrom) + "Test"));

            IEnumerable<int> sourceSet = new List<int>()
            {
                1
            };
            int sourceSetCount = sourceSet.Count();

            using NativeParallelHashSet<int> destinationSet = new NativeParallelHashSet<int>(3, Allocator.Persistent)
            {
                2, 3, 4
            };
            int originalDestinationCount = destinationSet.Count();

            destinationSet.CopyFrom(sourceSet);

            Assert.That(sourceSet.Count(), Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Capacity, Is.EqualTo(originalDestinationCount));
            Assert.That(destinationSet.Count(), Is.EqualTo(sourceSetCount));
            foreach (int item in sourceSet)
            {
                Assert.That(destinationSet.Contains(item), Is.True);
            }
        }

        [Test]
        public static void CopyFromTest_IEnumerable_LargerSource()
        {
            Assert.That(nameof(CopyFromTest_IEnumerable_LargerSource), Does.StartWith(nameof(NativeParallelHashSetExtension.CopyFrom) + "Test"));

            IEnumerable<int> sourceSet = new List<int>()
            {
                1, 2, 3
            };
            int sourceSetCount = sourceSet.Count();

            using NativeParallelHashSet<int> destinationSet = new NativeParallelHashSet<int>(1, Allocator.Persistent)
            {
                4
            };

            destinationSet.CopyFrom(sourceSet);

            Assert.That(sourceSet.Count(), Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Capacity, Is.EqualTo(sourceSetCount));
            Assert.That(destinationSet.Count(), Is.EqualTo(sourceSetCount));
            foreach (int item in sourceSet)
            {
                Assert.That(destinationSet.Contains(item), Is.True);
            }
        }
    }
}