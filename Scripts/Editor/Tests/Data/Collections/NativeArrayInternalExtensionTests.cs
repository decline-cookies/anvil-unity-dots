using Anvil.Unity.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Tests.Data
{
    public static class NativeArrayInternalExtensionTests
    {
        // ----- GetAllocator ----- //
        [Test]
        public static void GetAllocatorTest()
        {
            Assert.That(nameof(GetAllocatorTest), Does.StartWith(nameof(NativeArrayInternalExtension.GetAllocator) + "Test"));

            NativeArray<int> array = default;
            Assert.That(array.GetAllocator(), Is.EqualTo(Allocator.Invalid));

            array = new NativeArray<int>(1, Allocator.Persistent);
            Assert.That(array.GetAllocator(), Is.EqualTo(Allocator.Persistent));

            array.Dispose();
            Assert.That(array.GetAllocator(), Is.EqualTo(Allocator.Invalid));
        }
    }
}