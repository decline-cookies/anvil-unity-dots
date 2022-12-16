using Anvil.Unity.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class NativeParallelHashMapInternalExtensionTests
    {
        // ----- GetAllocator ----- //
        [Test]
        public static void GetAllocatorTest()
        {
            Assert.That(nameof(GetAllocatorTest), Does.StartWith(nameof(NativeParallelHashMapInternalExtension.GetAllocator) + "Test"));

            NativeParallelHashMap<int, int> hashMap = default;
            Assert.That(hashMap.GetAllocator(), Is.EqualTo(Allocator.Invalid));

            hashMap = new NativeParallelHashMap<int, int>(1, Allocator.Persistent);
            Assert.That(hashMap.GetAllocator(), Is.EqualTo(Allocator.Persistent));

            hashMap.Dispose();
            Assert.That(hashMap.GetAllocator(), Is.EqualTo(Allocator.Persistent));
        }

        // ----- GetKeyArray ----- //
        [Test]
        public static void GetKeyArrayTest()
        {
            Assert.That(nameof(GetKeyArrayTest), Does.StartWith(nameof(NativeParallelHashMapInternalExtension.GetKeyArray) + "Test"));

            using NativeParallelHashMap<FixedString32Bytes, int> hashMap = new NativeParallelHashMap<FixedString32Bytes, int>(3, Allocator.Persistent);
            using NativeArray<FixedString32Bytes> newArray = hashMap.GetKeyArray(Allocator.Persistent);

            using NativeArray<FixedString32Bytes> existingArray = new NativeArray<FixedString32Bytes>(hashMap.Count(), Allocator.Persistent);
            hashMap.GetKeyArray(existingArray);

            Assert.That(existingArray.Length, Is.EqualTo(newArray.Length));
            Assert.That(existingArray.Length, Is.EqualTo(hashMap.Count()));

            for (int i = 0; i < newArray.Length; i++)
            {
                Assert.That(existingArray[i], Is.EqualTo(newArray[i]));
                Assert.That(hashMap.ContainsKey(existingArray[i]), Is.True);
            }
        }

        // ----- GetValueArray ----- //
        [Test]
        public static void GetValueArrayTest()
        {
            Assert.That(nameof(GetValueArrayTest), Does.StartWith(nameof(NativeParallelHashMapInternalExtension.GetValueArray) + "Test"));

            using NativeParallelHashMap<FixedString32Bytes, int> hashMap = new NativeParallelHashMap<FixedString32Bytes, int>(3, Allocator.Persistent);
            using NativeArray<int> newArray = hashMap.GetValueArray(Allocator.Persistent);

            using NativeArray<int> existingArray = new NativeArray<int>(hashMap.Count(), Allocator.Persistent);
            hashMap.GetValueArray(existingArray);

            Assert.That(existingArray.Length, Is.EqualTo(newArray.Length));
            Assert.That(existingArray.Length, Is.EqualTo(hashMap.Count()));

            for (int i = 0; i < newArray.Length; i++)
            {
                Assert.That(existingArray[i], Is.EqualTo(newArray[i]));
            }
        }

        // ----- GetKeyValueArrays ----- //
        [Test]
        public static void GetKeyValueArraysTest()
        {
            Assert.That(nameof(GetKeyValueArraysTest), Does.StartWith(nameof(NativeParallelHashMapInternalExtension.GetKeyValueArrays) + "Test"));

            using NativeParallelHashMap<FixedString32Bytes, int> hashMap = new NativeParallelHashMap<FixedString32Bytes, int>(3, Allocator.Persistent);
            using NativeKeyValueArrays<FixedString32Bytes, int> newArray = hashMap.GetKeyValueArrays(Allocator.Persistent);

            using NativeKeyValueArrays<FixedString32Bytes, int> existingArray = new NativeKeyValueArrays<FixedString32Bytes, int>(hashMap.Count(), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            hashMap.GetKeyValueArrays(existingArray);

            Assert.That(existingArray.Length, Is.EqualTo(newArray.Length));
            Assert.That(existingArray.Length, Is.EqualTo(hashMap.Count()));

            for (int i = 0; i < newArray.Length; i++)
            {
                Assert.That(existingArray.Keys[i], Is.EqualTo(newArray.Keys[i]));
                Assert.That(existingArray.Values[i], Is.EqualTo(newArray.Values[i]));
                Assert.That(hashMap.ContainsKey(existingArray.Keys[i]), Is.True);
            }
        }
    }
}