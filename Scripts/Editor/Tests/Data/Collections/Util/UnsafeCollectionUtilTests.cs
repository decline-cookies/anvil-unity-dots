using System;
using Anvil.Unity.DOTS.Data;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class UnsafeCollectionUtilTests
    {
        // ----- FloodClearBuffer ----- //
        [Test]
        public static unsafe void FloodClearBufferTest_All()
        {
            Assert.That(nameof(FloodClearBufferTest_All), Does.StartWith(nameof(UnsafeCollectionUtil.FloodClearBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            UnsafeCollectionUtil.FloodClearBuffer<int>(array.GetUnsafePtr(), 0, array.Length);

            foreach (int item in array)
            {
                Assert.That(item, Is.EqualTo(default(int)));
            }
        }

        [Test]
        public static unsafe void FloodClearBufferTest_Range()
        {
            Assert.That(nameof(FloodClearBufferTest_Range), Does.StartWith(nameof(UnsafeCollectionUtil.FloodClearBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            UnsafeCollectionUtil.FloodClearBuffer<int>(array.GetUnsafePtr(), 1, 2);

            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(default(int)));
            Assert.That(array[2], Is.EqualTo(default(int)));
            Assert.That(array[3], Is.EqualTo(4));
        }

        [Test]
        public static unsafe void FloodClearBufferTest_Range_InvalidParams()
        {
            Assert.That(nameof(FloodClearBufferTest_Range_InvalidParams), Does.StartWith(nameof(UnsafeCollectionUtil.FloodClearBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodClearBuffer<int>(array.GetUnsafePtr(), -1, 3));
            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodClearBuffer<int>(array.GetUnsafePtr(), 2, 0));
            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodClearBuffer<int>(array.GetUnsafePtr(), 2, -1));
        }

        // ----- FloodSetBuffer ----- //
        [Test]
        public static unsafe void FloodSetBufferTest_All()
        {
            Assert.That(nameof(FloodSetBufferTest_All), Does.StartWith(nameof(UnsafeCollectionUtil.FloodSetBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            UnsafeCollectionUtil.FloodSetBuffer(array.GetUnsafePtr(), 0, array.Length, 10);

            foreach (int item in array)
            {
                Assert.That(item, Is.EqualTo(10));
            }
        }

        [Test]
        public static unsafe void FloodSetBufferTest_Range()
        {
            Assert.That(nameof(FloodSetBufferTest_Range), Does.StartWith(nameof(UnsafeCollectionUtil.FloodSetBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            UnsafeCollectionUtil.FloodSetBuffer(array.GetUnsafePtr(), 1, 2, 10);

            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(10));
            Assert.That(array[2], Is.EqualTo(10));
            Assert.That(array[3], Is.EqualTo(4));
        }

        [Test]
        public static unsafe void FloodSetBufferTest_Range_InvalidParams()
        {
            Assert.That(nameof(FloodSetBufferTest_Range_InvalidParams), Does.StartWith(nameof(UnsafeCollectionUtil.FloodSetBuffer) + "Test"));

            using NativeArray<int> array = new NativeArray<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4
                },
                Allocator.Persistent);

            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodSetBuffer<int>(array.GetUnsafePtr(), -1, 3, 10));
            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodSetBuffer<int>(array.GetUnsafePtr(), 2, 0, 10));
            Assert.Throws<Exception>(() => UnsafeCollectionUtil.FloodSetBuffer<int>(array.GetUnsafePtr(), 2, -1, 10));
        }
    }
}
