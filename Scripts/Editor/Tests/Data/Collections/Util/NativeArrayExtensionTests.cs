using System;
using Anvil.Unity.DOTS.Data;
using NUnit.Framework;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class NativeArrayExtensionTests
    {
        // ----- FloodClear ----- //
        [Test]
        public static void FloodClearTest_All()
        {
            Assert.That(nameof(FloodClearTest_All), Does.StartWith(nameof(NativeArrayExtension.FloodClear) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            array.FloodClear();

            foreach (int item in array)
            {
                Assert.That(item, Is.EqualTo(default(int)));
            }
        }

        [Test]
        public static void FloodClearTest_Range()
        {
            Assert.That(nameof(FloodClearTest_Range), Does.StartWith(nameof(NativeArrayExtension.FloodClear) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            array.FloodClear(1, 2);

            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(default(int)));
            Assert.That(array[2], Is.EqualTo(default(int)));
            Assert.That(array[3], Is.EqualTo(4));
        }

        [Test]
        public static void FloodClearTest_Range_OutOfRange()
        {
            Assert.That(nameof(FloodClearTest_Range_OutOfRange), Does.StartWith(nameof(NativeArrayExtension.FloodClear) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            Assert.Throws<Exception>(() => array.FloodClear(4, 1));
            Assert.Throws<Exception>(() => array.FloodClear(3, 2));
        }

        // ----- FloodSet ----- //
        [Test]
        public static void FloodSetTest_All()
        {
            Assert.That(nameof(FloodSetTest_All), Does.StartWith(nameof(NativeArrayExtension.FloodSet) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            array.FloodSet(10);

            foreach (int item in array)
            {
                Assert.That(item, Is.EqualTo(10));
            }
        }

        [Test]
        public static void FloodSetTest_Range()
        {
            Assert.That(nameof(FloodSetTest_Range), Does.StartWith(nameof(NativeArrayExtension.FloodSet) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            array.FloodSet(1, 2, 10);

            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(10));
            Assert.That(array[2], Is.EqualTo(10));
            Assert.That(array[3], Is.EqualTo(4));
        }

        [Test]
        public static void FloodSetTest_Range_OutOfRange()
        {
            Assert.That(nameof(FloodSetTest_Range), Does.StartWith(nameof(NativeArrayExtension.FloodSet) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(new []{1, 2, 3, 4}, Allocator.Persistent);

            Assert.Throws<Exception>(() => array.FloodSet(4, 1, 10));
            Assert.Throws<Exception>(() => array.FloodSet(3, 2, 10));
        }
    }
}