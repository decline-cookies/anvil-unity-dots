using Anvil.Unity.DOTS.Collections;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.Tests;
using Unity.Jobs;

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

        [Test]
        public void PopulateInts([Values(1, 100, 200)] int count, [Values(1, 3, 10)] int batchSize)
        {
            NativeArray<int> sourceInts = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; ++i)
            {
                sourceInts[i] = i;
            }

            UnsafeThreadedQueue<int> unsafeThreadedQueue = new UnsafeThreadedQueue<int>(count, Allocator.TempJob);

            WriteIntsJob writeJob = new WriteIntsJob(unsafeThreadedQueue.AsWriter(), sourceInts);
            JobHandle writeJobHandle = writeJob.ScheduleBatch(count, batchSize);

            ReadIntsJob readJob = new ReadIntsJob(unsafeThreadedQueue.AsReader(), sourceInts);
            JobHandle readJobHandle = readJob.ScheduleBatch(count, batchSize, writeJobHandle);
            
            readJobHandle.Complete();
            
            unsafeThreadedQueue.Dispose();
            sourceInts.Dispose();
        }


        [BurstCompile(CompileSynchronously = true)]
        private struct WriteIntsJob : IJobParallelForBatch
        {
            [ReadOnly] private readonly NativeArray<int> m_SourceInts;
            [WriteOnly] private UnsafeThreadedQueue<int>.Writer m_Writer;

            public WriteIntsJob(UnsafeThreadedQueue<int>.Writer writer, NativeArray<int> sourceInts)
            {
                m_Writer = writer;
                m_SourceInts = sourceInts;
            }

            public void Execute(int startIndex, int count)
            {
                int endIndex = startIndex + count;
                for (int i = startIndex; i < endIndex; ++i)
                {
                    m_Writer.Write(m_SourceInts[i]);
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ReadIntsJob : IJobParallelForBatch
        {
            [ReadOnly] private readonly NativeArray<int> m_SourceInts;
            [ReadOnly] private UnsafeThreadedQueue<int>.Reader m_Reader;

            public ReadIntsJob(UnsafeThreadedQueue<int>.Reader reader, NativeArray<int> sourceInts)
            {
                m_Reader = reader;
                m_SourceInts = sourceInts;
            }

            public void Execute(int startIndex, int count)
            {
                while (m_Reader.CanRead())
                {
                    int peekedValue = m_Reader.Peek();
                    int value = m_Reader.Read();
                    Assert.AreEqual(m_SourceInts[value], value);
                    Assert.AreEqual(m_SourceInts[peekedValue], peekedValue);  
                }
            }
        }
    }
}

