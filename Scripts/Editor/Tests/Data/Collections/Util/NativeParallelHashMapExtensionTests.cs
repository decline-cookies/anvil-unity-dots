using System.Linq;
using Anvil.Unity.DOTS.Data;
using NUnit.Framework;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class NativeParallelHashMapExtensionTests
    {
        // ----- Remove ----- //
        [Test]
        public static void RemoveTest_ExistingKey()
        {
            Assert.That(nameof(RemoveTest_ExistingKey), Does.StartWith(nameof(NativeParallelHashMapExtension.Remove) + "Test"));

            using NativeParallelHashMap<FixedString32Bytes, int> hashMap = new NativeParallelHashMap<FixedString32Bytes, int>(2, Allocator.Persistent)
            {
                {"one", 1},
                {"two", 2}
            };

            bool result = hashMap.Remove("one", out int removedValue);

            Assert.That(removedValue, Is.EqualTo(1));
            Assert.That(result, Is.True);

            Assert.That(hashMap.ContainsKey("one"), Is.False);
            Assert.That(hashMap.ContainsKey("two"), Is.True);
            Assert.That(hashMap.Count(), Is.EqualTo(1));
        }

        [Test]
        public static void RemoveTest_MissingKey()
        {
            Assert.That(nameof(RemoveTest_ExistingKey), Does.StartWith(nameof(NativeParallelHashMapExtension.Remove) + "Test"));

            using NativeParallelHashMap<FixedString32Bytes, int> hashMap = new NativeParallelHashMap<FixedString32Bytes, int>(2, Allocator.Persistent)
            {
                {"one", 1},
                {"two", 2}
            };

            bool result = hashMap.Remove("three", out int removedValue);

            Assert.That(removedValue, Is.EqualTo(default(int)));
            Assert.That(result, Is.False);

            Assert.That(hashMap.ContainsKey("one"), Is.True);
            Assert.That(hashMap.ContainsKey("two"), Is.True);
            Assert.That(hashMap.Count(), Is.EqualTo(2));

        }
    }
}