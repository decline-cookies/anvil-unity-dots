using Anvil.Unity.DOTS.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Tests.Data
{
    //NOTE - Taken from: https://github.com/Amarcolina/NativeHeap (MIT LICENCE)
    //These tests have been slightly modified to fit our convention but the content 
    //of the tests themselves are copy/paste. Many tests were removed because they
    //rely on safety checks which the UnsafeHeap doesn't have.
    
    public static class UnsafeHeapTests
    {
        private static UnsafeHeap<int, Min> s_Heap;

        [SetUp]
        public static void SetUp() {
            s_Heap = new UnsafeHeap<int, Min>(Allocator.Persistent);
        }

        [TearDown]
        public static void TearDown() {
            s_Heap.Dispose();
        }

        [Test]
        public static void TestInsertionAndRemoval() {
            List<int> list = new List<int>();
            for (int i = 0; i < 100; i++) {
                s_Heap.Insert(i);
                list.Add(i);
            }

            for (int i = 0; i < 1000; i++) {
                var min = s_Heap.Pop();
                Assert.That(min, Is.EqualTo(list.Min()));

                list.Remove(min);

                int toInsert = Random.Range(0, 100);
                s_Heap.Insert(toInsert);
                list.Add(toInsert);
            }
        }

        [Test]
        public static void TestCanRemoveUsingIndex() {
            List<(int, UnsafeHeap<int, Min>.UnsafeHeapIndex)> itemRefs = new List<(int, UnsafeHeap<int, Min>.UnsafeHeapIndex)>();
            for (int i = 0; i < 100; i++) {
                int value = Random.Range(0, 1000);
                var itemRef = s_Heap.Insert(value);
                itemRefs.Add((value, itemRef));
            }

            foreach ((var value, var itemRef) in itemRefs) {
                var item = s_Heap.Remove(itemRef);
                Assert.That(item, Is.EqualTo(value));
            }
        }

        [Test]
        public static void TestPeekIsSameAsPop() {
            for (int i = 0; i < 100; i++) {
                s_Heap.Insert(Random.Range(0, 1000));
            }

            while (s_Heap.Count > 0) {
                int value1 = s_Heap.Peek();
                int value2 = s_Heap.Pop();

                Assert.That(value1, Is.EqualTo(value2));
            }
        }

        [Test]
        public static void TestRemoveFromMiddle() {
            List<int> items = new List<int>();
            int GetValue() => Random.value > 0.5f ? Random.Range(0, 1000) : Random.Range(1001, 2000);

            for (int i = 0; i < 100; i++) {
                var value = GetValue();
                items.Add(value);
                s_Heap.Insert(value);
            }

            var index = s_Heap.Insert(1000);

            for (int i = 0; i < 100; i++) {
                var value = GetValue();
                items.Add(value);
                s_Heap.Insert(value);
            }

            s_Heap.Remove(index);

            foreach (var item in items.OrderBy(i => i)) {
                Assert.That(s_Heap.Pop(), Is.EqualTo(item));
            }
        }

        [Test]
        public static void TestCopyReflectsChanges() {
            var heapCopy = s_Heap;
            heapCopy.Insert(5);
            heapCopy.Capacity *= 2;

            Assert.That(s_Heap.Peek(), Is.EqualTo(5));

            heapCopy.Pop();

            Assert.That(s_Heap.Count, Is.Zero);
            Assert.That(s_Heap.Capacity, Is.EqualTo(heapCopy.Capacity));
        }
    }
}
